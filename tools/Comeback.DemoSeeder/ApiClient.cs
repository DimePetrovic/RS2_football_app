namespace Comeback.DemoSeeder;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Tanak HTTP sloj ka gateway-u i MailDev-u. Registracioni tok (MailDev inbox, regex linka,
/// resend pri timeout-u) je preuzet iz e2e LiveStackFixture — to je provereni recept za zivi stack.
/// </summary>
public sealed class ApiClient : IDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _gateway;
    private readonly HttpClient _mailDev;

    public ApiClient(string gatewayUrl, string mailDevUrl)
    {
        GatewayUrl = gatewayUrl;
        MailDevUrl = mailDevUrl;
        _gateway = new HttpClient { BaseAddress = new Uri(gatewayUrl), Timeout = TimeSpan.FromSeconds(30) };
        _mailDev = new HttpClient { BaseAddress = new Uri(mailDevUrl), Timeout = TimeSpan.FromSeconds(30) };
    }

    public string GatewayUrl { get; }

    public string MailDevUrl { get; }

    public void Dispose()
    {
        _gateway.Dispose();
        _mailDev.Dispose();
    }

    /// <summary>Null kada je stack dostupan; inace razlog za prekid.</summary>
    public async Task<string?> ProbeAsync()
    {
        try
        {
            // Bilo kakav HTTP odgovor (i 401) dokazuje da su gateway i servis zivi.
            await _gateway.GetAsync("/api/profiles/me");
        }
        catch (Exception ex)
        {
            return $"Gateway nije dostupan na {GatewayUrl} ({ex.GetType().Name}). " +
                   "Pokreni `docker compose up -d` iz infra/docker.";
        }

        try
        {
            await _mailDev.GetAsync("/email");
        }
        catch (Exception ex)
        {
            return $"MailDev nije dostupan na {MailDevUrl} ({ex.GetType().Name}).";
        }

        return null;
    }

    // ── Osnovni pozivi ───────────────────────────────────────────────────

    public async Task<HttpResponseMessage> PostAsync(string path, object? body, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body ?? new { }, options: Json),
        };
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _gateway.SendAsync(request);
    }

    public async Task<JsonElement?> GetJsonAsync(string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _gateway.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>(Json);

    /// <summary>
    /// Anketira dok poziv ne vrati ne-null. Efekti izmedju servisa putuju preko RabbitMQ-a
    /// (MassTransit EF outbox anketira bazu na ~10s), pa je jedno trenutno citanje nepouzdano.
    /// </summary>
    public static async Task<T> WaitForAsync<T>(
        Func<Task<T?>> attempt,
        string description,
        int timeoutSeconds = 30) where T : class
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var result = await attempt();
            if (result is not null) return result;
            await Task.Delay(500);
        }

        throw new TimeoutException($"Nije docekano za {timeoutSeconds}s: {description}");
    }

    // ── Registracija korisnika ───────────────────────────────────────────

    public sealed record SeededUser(Guid UserId, string Token, bool Created);

    /// <summary>
    /// Idempotentno obezbedjuje korisnika: prvo pokusa login (ponovni run), pa registraciju sa
    /// potvrdom mejla; ako je raniji run stao izmedju registracije i potvrde, trazi ponovno
    /// slanje verifikacionog mejla i nastavlja odatle.
    /// </summary>
    public async Task<SeededUser> EnsureUserAsync(DemoUser user)
    {
        var login = await PostAsync("/api/auth/login", new { email = user.Email, password = DemoData.Password });
        if (login.IsSuccessStatusCode)
        {
            var body = await ReadJsonAsync(login);
            return new SeededUser(
                body.GetProperty("userId").GetGuid(), body.GetProperty("accessToken").GetString()!, Created: false);
        }

        var register = await PostAsync("/api/auth/register", new
        {
            email = user.Email,
            username = user.Username,
            password = DemoData.Password,
            confirmPassword = DemoData.Password,
        });

        if (!register.IsSuccessStatusCode)
        {
            // Nalog postoji, a login nije prosao — verovatno registracija bez potvrdjenog mejla.
            var resend = await PostAsync("/api/auth/resend-confirmation", new { email = user.Email });
            if (!resend.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Nalog {user.Email} postoji, ali login i ponovna verifikacija ne prolaze " +
                    $"(register {(int)register.StatusCode}, resend {(int)resend.StatusCode}). " +
                    "Ako je baza iz starijeg seta podataka, pokreni `docker compose down -v` pa ispocetka.");
        }

        var (userId, token) = await WaitForVerificationLinkAsync(user.Email);

        var complete = await PostAsync("/api/auth/complete-registration", new
        {
            userId,
            token,
            firstName = user.FirstName,
            lastName = user.LastName,
            dateOfBirth = user.DateOfBirth,
            preferredPosition = user.PreferredPosition,
            canPlayGoalkeeper = user.CanPlayGoalkeeper,
            youthSeasons = user.YouthSeasons,
            seniorSeasons = user.SeniorSeasons,
            nationality = "RS",
        });

        if (!complete.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"complete-registration za {user.Email} nije prosao ({(int)complete.StatusCode}).");

        var completed = await ReadJsonAsync(complete);
        return new SeededUser(
            completed.GetProperty("userId").GetGuid(),
            completed.GetProperty("accessToken").GetString()!,
            Created: true);
    }

    /// <summary>Ceka da profil nastane u Profile servisu (event auth → profile).</summary>
    public async Task WaitForProfileAsync(string token, string email)
        => await WaitForAsync(
            async () => await GetJsonAsync("/api/profiles/me", token) is null ? null : "ok",
            $"profil za {email}",
            timeoutSeconds: 60);

    /// <summary>
    /// Cita verifikacioni link iz MailDev sandudceta. MailDev-ov SMTP ume da otkaze pod
    /// naletom mejlova — tada se mejl gubi, pa jednom trazimo ponovno slanje i sacekamo opet.
    /// </summary>
    private async Task<(string UserId, string Token)> WaitForVerificationLinkAsync(string email)
    {
        try
        {
            return await ReadVerificationLinkAsync(email, timeoutSeconds: 60);
        }
        catch (TimeoutException)
        {
            var resend = await PostAsync("/api/auth/resend-confirmation", new { email });
            resend.EnsureSuccessStatusCode();
            return await ReadVerificationLinkAsync(email, timeoutSeconds: 60);
        }
    }

    private async Task<(string UserId, string Token)> ReadVerificationLinkAsync(string email, int timeoutSeconds)
    {
        var html = await WaitForAsync(
            async () =>
            {
                var inbox = await _mailDev.GetFromJsonAsync<JsonElement[]>("/email", Json) ?? [];
                // Od najnovijeg ka starijem — ponovni run moze da zatekne starije mejlove za istu adresu.
                foreach (var message in Enumerable.Reverse(inbox))
                {
                    if (!message.TryGetProperty("to", out var recipients)) continue;
                    var matches = recipients.EnumerateArray().Any(r =>
                        r.TryGetProperty("address", out var a) &&
                        string.Equals(a.GetString(), email, StringComparison.OrdinalIgnoreCase));
                    if (matches && message.TryGetProperty("html", out var body))
                        return body.GetString();
                }

                return null;
            },
            $"verifikacioni mejl za {email}",
            timeoutSeconds);

        var match = Regex.Match(
            html, @"complete-profile\?userId=(?<id>[^&""]+)&(?:amp;)?token=(?<token>[^""&]+)");
        if (!match.Success)
            throw new InvalidOperationException($"Verifikacioni link nije pronadjen u mejlu za {email}.");

        return (match.Groups["id"].Value, Uri.UnescapeDataString(match.Groups["token"].Value));
    }
}
