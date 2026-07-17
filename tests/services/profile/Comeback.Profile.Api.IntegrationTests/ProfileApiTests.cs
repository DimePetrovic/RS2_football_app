namespace Comeback.Profile.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

public sealed class ProfileApiTests : IClassFixture<ProfileApiFactory>
{
    private readonly ProfileApiFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ProfileApiTests(ProfileApiFactory factory) => _factory = factory;

    private HttpClient ClientFor(Guid userId, string role = "Player")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId, role: role));
        return client;
    }

    [Fact]
    public async Task CreateGroup_WithMember_ReturnsGroupWithCaptainAndMember()
    {
        var captain = Guid.NewGuid();
        var member = Guid.NewGuid();
        await _factory.SeedProfileAsync(captain, "kapiten");
        await _factory.SeedProfileAsync(member, "clan");
        var client = ClientFor(captain);

        var create = await client.PostAsJsonAsync("/api/groups",
            new { name = "Tim Sever", avatarUrl = (string?)null, memberUserIds = new[] { member } }, Json);
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var summary = await create.Content.ReadFromJsonAsync<JsonElement>(Json);
        summary.GetProperty("name").GetString().Should().Be("Tim Sever");
        summary.GetProperty("myRole").GetString().Should().Be("Captain");
        var groupId = summary.GetProperty("id").GetGuid();

        var detail = await (await client.GetAsync($"/api/groups/{groupId}"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var members = detail.GetProperty("members").EnumerateArray().ToList();
        members.Should().HaveCount(2);
        members.Should().ContainSingle(m => m.GetProperty("role").GetString() == "Captain");
    }

    [Fact]
    public async Task CreateGroup_WithAdminMember_IsRejected()
    {
        var captain = Guid.NewGuid();
        var admin = Guid.NewGuid();
        await _factory.SeedProfileAsync(captain, "kapiten2");
        await _factory.SeedProfileAsync(admin, "admin", role: "Admin");
        var client = ClientFor(captain);

        var response = await client.PostAsJsonAsync("/api/groups",
            new { name = "Grupa", avatarUrl = (string?)null, memberUserIds = new[] { admin } }, Json);

        // "An administrator cannot be a group member" -> BusinessRuleException (422).
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task FollowThenStatus_ReflectsFollowing()
    {
        var follower = Guid.NewGuid();
        var target = Guid.NewGuid();
        await _factory.SeedProfileAsync(follower, "pratilac");
        await _factory.SeedProfileAsync(target, "cilj");
        var client = ClientFor(follower);

        (await client.PostAsync($"/api/profiles/{target}/follow", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var status = await (await client.GetAsync($"/api/profiles/{target}/follow-status"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        status.GetProperty("isFollowing").GetBoolean().Should().BeTrue();

        await client.DeleteAsync($"/api/profiles/{target}/follow");
        var afterUnfollow = await (await client.GetAsync($"/api/profiles/{target}/follow-status"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        afterUnfollow.GetProperty("isFollowing").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Search_ExcludesAdminProfiles()
    {
        var searcher = Guid.NewGuid();
        await _factory.SeedProfileAsync(searcher, "trazilac");
        await _factory.SeedProfileAsync(Guid.NewGuid(), "petar_player");
        await _factory.SeedProfileAsync(Guid.NewGuid(), "petar_admin", role: "Admin");
        var client = ClientFor(searcher);

        var results = await (await client.GetAsync("/api/profiles/search?query=petar"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);

        var usernames = results.EnumerateArray()
            .Select(p => p.GetProperty("username").GetString())
            .ToList();
        usernames.Should().Contain("petar_player");
        usernames.Should().NotContain("petar_admin");
    }

    [Fact]
    public async Task GetMyProfile_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/profiles/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
