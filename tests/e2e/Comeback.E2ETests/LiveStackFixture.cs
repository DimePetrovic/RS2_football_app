namespace Comeback.E2ETests;

using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

/// <summary>
/// Shared handle on the running system. Unlike the per-service integration tests — which boot one
/// API in-process and fake away messaging — these tests talk to the real stack over the network:
/// gateway → service → Postgres, and the RabbitMQ hops between services.
///
/// Start it with <c>docker compose up -d</c> from <c>infra/docker</c>. When the stack is not
/// reachable the tests skip instead of failing, so <c>dotnet test</c> stays usable without it.
/// </summary>
public sealed class LiveStackFixture : IAsyncLifetime
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static string GatewayUrl =>
        Environment.GetEnvironmentVariable("E2E_GATEWAY_URL") ?? "http://localhost:5000";

    private static string MailDevUrl =>
        Environment.GetEnvironmentVariable("E2E_MAILDEV_URL") ?? "http://localhost:1080";

    public HttpClient Gateway { get; private set; } = null!;
    public HttpClient MailDev { get; private set; } = null!;

    /// <summary>Null while the stack is reachable; otherwise the reason to skip.</summary>
    public string? SkipReason { get; private set; }

    public async Task InitializeAsync()
    {
        Gateway = new HttpClient { BaseAddress = new Uri(GatewayUrl), Timeout = TimeSpan.FromSeconds(30) };
        MailDev = new HttpClient { BaseAddress = new Uri(MailDevUrl), Timeout = TimeSpan.FromSeconds(30) };

        SkipReason = await ProbeAsync();
    }

    public Task DisposeAsync()
    {
        Gateway.Dispose();
        MailDev.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Fails fast with an actionable message when the stack is not up. These tests are not part of
    /// <c>Comeback.sln</c>, so they only run when someone deliberately points them at a live system —
    /// in that case a hard failure is more useful than a silent skip.
    /// </summary>
    public void RequireStack()
    {
        if (SkipReason is not null) throw new InvalidOperationException(SkipReason);
    }

    private async Task<string?> ProbeAsync()
    {
        try
        {
            // Any HTTP answer proves gateway + service are alive; 401 is the expected one here.
            await Gateway.GetAsync("/api/profiles/me");
        }
        catch (Exception ex)
        {
            return $"Gateway nije dostupan na {GatewayUrl} ({ex.GetType().Name}). " +
                   "Pokreni `docker compose up -d` iz infra/docker.";
        }

        try
        {
            await MailDev.GetAsync("/email");
        }
        catch (Exception ex)
        {
            return $"MailDev nije dostupan na {MailDevUrl} ({ex.GetType().Name}).";
        }

        return null;
    }

    // ── Pomocne metode ───────────────────────────────────────────────────

    /// <summary>
    /// Polls until <paramref name="attempt"/> returns a non-null value. Cross-service effects travel
    /// over RabbitMQ, so they are eventually consistent — a single immediate read would be flaky.
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

    public sealed record ConfirmedUser(string Email, string Username, string AccessToken);

    private readonly SemaphoreSlim _sharedUserLock = new(1, 1);
    private readonly Dictionary<string, ConfirmedUser> _sharedUsers = [];

    /// <summary>
    /// A confirmed user created once per run and shared between tests. Every registration costs one
    /// verification email, and MailDev's SMTP server starts timing out under a burst of them — so
    /// tests that merely need "some logged-in user" reuse these instead of registering their own.
    /// </summary>
    public async Task<ConfirmedUser> GetSharedUserAsync(string slot)
    {
        await _sharedUserLock.WaitAsync();
        try
        {
            if (_sharedUsers.TryGetValue(slot, out var existing)) return existing;

            var created = await CreateConfirmedUserAsync();
            _sharedUsers[slot] = created;
            return created;
        }
        finally
        {
            _sharedUserLock.Release();
        }
    }

    /// <summary>
    /// Logs in as the admin account that the Auth service seeds on first start. Tests that only need
    /// "a valid token" use this instead of registering — no verification email, nothing to wait for.
    /// </summary>
    public async Task<string> LoginAsSeededAdminAsync()
    {
        var login = await Gateway.PostAsJsonAsync("/api/auth/login",
            new { email = "d7petrovic@gmail.com", password = "Test!234" }, Json);

        if (!login.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Prijava seed-ovanog admin naloga nije uspela ({(int)login.StatusCode}). " +
                "Ocekuje se da ga Auth servis napravi pri prvom pokretanju.");

        var body = await login.Content.ReadFromJsonAsync<JsonElement>(Json);
        return body.GetProperty("accessToken").GetString()!;
    }

    /// <summary>Registers a user, confirms the email through the MailDev link, and logs in.</summary>
    public async Task<ConfirmedUser> CreateConfirmedUserAsync()
    {
        var (email, username, password) = await RegisterAsync();
        var (userId, token) = await WaitForVerificationLinkAsync(email);

        var complete = await Gateway.PostAsJsonAsync("/api/auth/complete-registration", new
        {
            userId,
            token,
            firstName = "Petar",
            lastName = "Petrovic",
            dateOfBirth = "1998-05-14",
            preferredPosition = 1,
            canPlayGoalkeeper = false,
            youthSeasons = 2,
            seniorSeasons = 3,
            nationality = "RS",
        }, Json);
        complete.EnsureSuccessStatusCode();

        var login = await Gateway.PostAsJsonAsync("/api/auth/login", new { email, password }, Json);
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(Json);

        return new ConfirmedUser(email, username, body.GetProperty("accessToken").GetString()!);
    }

    /// <summary>Registers a user and returns the credentials, without confirming the email.</summary>
    public async Task<(string Email, string Username, string Password)> RegisterAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"e2e_{suffix}@test.com";
        var username = $"e2e{suffix}"[..18];
        const string password = "Test!234";

        var response = await Gateway.PostAsJsonAsync("/api/auth/register",
            new { email, username, password, confirmPassword = password }, Json);
        response.EnsureSuccessStatusCode();

        return (email, username, password);
    }

    /// <summary>
    /// Reads the verification link out of the MailDev inbox and pulls userId + token from it.
    ///
    /// MailDev's SMTP server times out under a burst of consecutive sends; the notification
    /// consumer then faults the message into <c>notification-email-verification-requested_error</c>
    /// and that one email is simply lost. Rather than pretend it cannot happen, this asks auth to
    /// resend once — exactly what a user staring at an empty inbox would do.
    /// </summary>
    public async Task<(string UserId, string Token)> WaitForVerificationLinkAsync(string email)
    {
        try
        {
            return await ReadVerificationLinkAsync(email, timeoutSeconds: 60);
        }
        catch (TimeoutException)
        {
            var resend = await Gateway.PostAsJsonAsync("/api/auth/resend-confirmation", new { email }, Json);
            resend.EnsureSuccessStatusCode();
            return await ReadVerificationLinkAsync(email, timeoutSeconds: 60);
        }
    }

    private async Task<(string UserId, string Token)> ReadVerificationLinkAsync(string email, int timeoutSeconds)
    {
        var html = await WaitForAsync(
            async () =>
            {
                var inbox = await MailDev.GetFromJsonAsync<JsonElement[]>("/email", Json) ?? [];
                // Newest first — a rerun can leave older messages for the same address behind.
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
            // Auth koristi MassTransit EF outbox (UseBusOutbox), cija delivery sluzba anketira
            // bazu na ~10s i salje u serijama. Uz RabbitMQ skok i SMTP, kratak timeout je pretesan.
            timeoutSeconds);

        var match = System.Text.RegularExpressions.Regex.Match(
            html, @"complete-profile\?userId=(?<id>[^&""]+)&(?:amp;)?token=(?<token>[^""&]+)");
        if (!match.Success)
            throw new InvalidOperationException($"Verifikacioni link nije pronadjen u mejlu za {email}.");

        return (match.Groups["id"].Value, Uri.UnescapeDataString(match.Groups["token"].Value));
    }
}

[CollectionDefinition(nameof(LiveStackCollection))]
public sealed class LiveStackCollection : ICollectionFixture<LiveStackFixture>;
