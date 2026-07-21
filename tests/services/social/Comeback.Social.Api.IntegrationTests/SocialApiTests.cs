namespace Comeback.Social.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Comeback.Social.Domain.Entities;
using FluentAssertions;
using Xunit;

public sealed class SocialApiTests : IClassFixture<SocialApiFactory>
{
    private readonly SocialApiFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public SocialApiTests(SocialApiFactory factory) => _factory = factory;

    private HttpClient ClientFor(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    private static Post NewPost() => Post.CreateMatchResultPost(
        Guid.NewGuid(), "Friday 8pm", 1, 0,
        [(FakeMatchDetailsClient.ScorerUserId, "Scorer")]);

    [Fact]
    public async Task GetFeed_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/feed?page=0&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPost_EnrichesPlayersWithGoalsAndScore()
    {
        var post = NewPost();
        await _factory.SeedPostAsync(post);
        var client = ClientFor(Guid.NewGuid());

        var response = await client.GetAsync($"/api/posts/{post.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        body.GetProperty("homeScore").GetInt32().Should().Be(1);
        body.GetProperty("awayScore").GetInt32().Should().Be(0);

        var players = body.GetProperty("players").EnumerateArray().ToList();
        players.Should().HaveCount(2);
        var scorer = players.Single(p => p.GetProperty("userId").GetGuid() == FakeMatchDetailsClient.ScorerUserId);
        scorer.GetProperty("goals").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task ToggleReaction_LikesThenUnlikes()
    {
        var post = NewPost();
        await _factory.SeedPostAsync(post);
        var user = Guid.NewGuid();
        var client = ClientFor(user);

        var like = await (await client.PostAsync($"/api/posts/{post.Id}/reactions", null))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        like.GetProperty("liked").GetBoolean().Should().BeTrue();

        var afterLike = await (await client.GetAsync($"/api/posts/{post.Id}"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        afterLike.GetProperty("likeCount").GetInt32().Should().Be(1);
        afterLike.GetProperty("likedByMe").GetBoolean().Should().BeTrue();

        var unlike = await (await client.PostAsync($"/api/posts/{post.Id}/reactions", null))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        unlike.GetProperty("liked").GetBoolean().Should().BeFalse();

        var afterUnlike = await (await client.GetAsync($"/api/posts/{post.Id}"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        afterUnlike.GetProperty("likeCount").GetInt32().Should().Be(0);
        afterUnlike.GetProperty("likedByMe").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task AddComment_ThenGetComments_ReturnsComment()
    {
        var post = NewPost();
        await _factory.SeedPostAsync(post);
        var client = ClientFor(Guid.NewGuid());

        var add = await client.PostAsJsonAsync($"/api/posts/{post.Id}/comments",
            new { content = "Great game, team!" }, Json);
        add.StatusCode.Should().Be(HttpStatusCode.OK);

        var comments = await (await client.GetAsync($"/api/posts/{post.Id}/comments"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        comments.EnumerateArray().Select(c => c.GetProperty("content").GetString())
            .Should().Contain("Great game, team!");
    }

    [Fact]
    public async Task GetFeed_ReturnsPostsInUsersFeed()
    {
        var user = Guid.NewGuid();
        var post = NewPost();
        await _factory.SeedPostAsync(post, feedForUser: user);
        var client = ClientFor(user);

        var feed = await (await client.GetAsync("/api/feed?page=0&pageSize=10"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);

        feed.EnumerateArray().Select(p => p.GetProperty("id").GetGuid())
            .Should().Contain(post.Id);
    }
}
