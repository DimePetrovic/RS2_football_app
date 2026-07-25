namespace Comeback.E2ETests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

/// <summary>
/// Spec 14.6, prvi tok: "registracija igraca, potvrda email adrese i kreiranje profila".
/// Prolazi kroz gateway → auth → RabbitMQ → notification → SMTP → MailDev, pa nazad kroz
/// auth → RabbitMQ → profile. Nijedan od ovih skokova ne postoji u per-servis testovima.
/// </summary>
[Collection(nameof(LiveStackCollection))]
public sealed class RegistrationFlowTests
{
    private readonly LiveStackFixture _stack;

    public RegistrationFlowTests(LiveStackFixture stack) => _stack = stack;

    [Fact]
    public async Task Registration_SendsVerificationEmail_AndUnlocksLoginOnlyAfterConfirmation()
    {
        _stack.RequireStack();
        var (email, _, password) = await _stack.RegisterAsync();

        // Dok email nije potvrdjen, nalog je u PendingEmailVerification -> prijava se odbija.
        var earlyLogin = await _stack.Gateway.PostAsJsonAsync("/api/auth/login",
            new { email, password }, LiveStackFixture.Json);
        earlyLogin.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Mejl stize tek posto auth objavi dogadjaj i notification servis ga obradi.
        var (userId, token) = await _stack.WaitForVerificationLinkAsync(email);

        var validate = await _stack.Gateway.GetFromJsonAsync<JsonElement>(
            $"/api/auth/validate-email-token?userId={userId}&token={Uri.EscapeDataString(token)}",
            LiveStackFixture.Json);
        validate.GetProperty("isValid").GetBoolean().Should().BeTrue();

        var complete = await _stack.Gateway.PostAsJsonAsync("/api/auth/complete-registration",
            CompleteRegistrationBody(userId, token), LiveStackFixture.Json);
        complete.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await _stack.Gateway.PostAsJsonAsync("/api/auth/login",
            new { email, password }, LiveStackFixture.Json);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(LiveStackFixture.Json);
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ConfirmedRegistration_CreatesProfileInAnotherService_AndMakesItSearchable()
    {
        _stack.RequireStack();
        var subject = await _stack.GetSharedUserAsync("a");

        // Profil pravi profile servis kada preko RabbitMQ-a stigne UserEmailConfirmed —
        // zato se ceka, umesto da se cita odmah.
        var profileJson = await LiveStackFixture.WaitForAsync(
            async () =>
            {
                using var request = Authorized(HttpMethod.Get, "/api/profiles/me", subject.AccessToken);
                var response = await _stack.Gateway.SendAsync(request);
                return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
            },
            $"profil za {subject.Username} preko UserEmailConfirmed dogadjaja");

        using var profile = JsonDocument.Parse(profileJson);
        profile.RootElement.GetProperty("username").GetString().Should().Be(subject.Username);

        // Pretraga izostavlja onoga ko pretrazuje (SearchProfilesQuery.ExcludeUserId),
        // pa profil trazi drugi korisnik — to je i stvarni scenario.
        var searcherToken = await _stack.LoginAsSeededAdminAsync();

        var found = await LiveStackFixture.WaitForAsync(
            async () =>
            {
                using var request = Authorized(
                    HttpMethod.Get, $"/api/profiles/search?query={subject.Username}", searcherToken);
                var response = await _stack.Gateway.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                var results = await response.Content.ReadFromJsonAsync<JsonElement>(LiveStackFixture.Json);
                return results.EnumerateArray()
                    .Any(p => p.GetProperty("username").GetString() == subject.Username) ? "found" : null;
            },
            $"pretraga profila po korisnickom imenu {subject.Username}");

        found.Should().Be("found");
    }

    [Fact]
    public async Task AccessTokenIssuedByAuth_IsAcceptedByProfileAndNotificationServices()
    {
        _stack.RequireStack();
        var accessToken = await _stack.LoginAsSeededAdminAsync();

        // Per-servis testovi koriste rucno sklopljene tokene; ovde token stvarno izdaje auth
        // servis, a prihvataju ga drugi servisi sa svojom JWT konfiguracijom.
        foreach (var path in new[] { "/api/profiles/me", "/api/notifications", "/api/notifications/unread-count" })
        {
            using var request = Authorized(HttpMethod.Get, path, accessToken);
            var response = await _stack.Gateway.SendAsync(request);

            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized, $"token treba da vazi za {path}");
            response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, $"token treba da vazi za {path}");
        }
    }

    [Fact]
    public async Task Gateway_RejectsUnauthenticatedRequests_ForEveryProtectedService()
    {
        _stack.RequireStack();

        foreach (var path in new[] { "/api/profiles/me", "/api/notifications", "/api/matches" })
        {
            var response = await _stack.Gateway.GetAsync(path);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"{path} ne sme biti javan");
        }
    }

    // ── Pomocne metode ───────────────────────────────────────────────────

    private static object CompleteRegistrationBody(string userId, string token) => new
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
    };

    private static HttpRequestMessage Authorized(HttpMethod method, string path, string accessToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}
