namespace Comeback.Auth.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

public sealed class AuthApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public AuthApiTests(AuthApiFactory factory) => _factory = factory;

    private static object RegisterBody(string email, string username, string password = "Test!234")
        => new { email, username, password, confirmPassword = password };

    private static string RefreshCookie(HttpResponseMessage login)
    {
        var setCookie = login.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith("X-Refresh-Token=", StringComparison.Ordinal));
        return setCookie.Split(';')[0]; // "X-Refresh-Token=<value>"
    }

    [Fact]
    public async Task Register_NewUser_IsAccepted()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            RegisterBody($"new_{Guid.NewGuid():N}@test.com", $"user_{Guid.NewGuid():N}"[..20]), Json);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = $"dup_{Guid.NewGuid():N}@test.com";

        (await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, $"u{Guid.NewGuid():N}"[..18]), Json))
            .StatusCode.Should().Be(HttpStatusCode.Accepted);

        var second = await client.PostAsJsonAsync("/api/auth/register",
            RegisterBody(email, $"u{Guid.NewGuid():N}"[..18]), Json);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_UnconfirmedUser_IsForbidden()
    {
        var client = _factory.CreateClient();
        var email = $"unconf_{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, $"u{Guid.NewGuid():N}"[..18]), Json);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "Test!234" }, Json);

        // The account is in PendingEmailVerification status -> login is forbidden.
        login.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsNotFound()
    {
        var email = $"wrongpw_{Guid.NewGuid():N}@test.com";
        await _factory.CreateActiveUserAsync(email, $"u{Guid.NewGuid():N}"[..18], "Test!234");
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "PogresnaLozinka9" }, Json);

        login.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LoginThenRefresh_ReturnsAccessTokens()
    {
        var email = $"active_{Guid.NewGuid():N}@test.com";
        await _factory.CreateActiveUserAsync(email, $"u{Guid.NewGuid():N}"[..18], "Test!234");
        var client = _factory.CreateClient();

        // Login — access token in the body, refresh token in an HttpOnly cookie.
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "Test!234" }, Json);
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>(Json);
        loginBody.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        loginBody.GetProperty("email").GetString().Should().Be(email);

        // Refresh — the cookie is passed manually (a Secure cookie does not travel over the HTTP test server).
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", RefreshCookie(login));
        var refresh = await client.SendAsync(refreshRequest);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshBody = await refresh.Content.ReadFromJsonAsync<JsonElement>(Json);
        refreshBody.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Revoke_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().PostAsync("/api/auth/revoke", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
