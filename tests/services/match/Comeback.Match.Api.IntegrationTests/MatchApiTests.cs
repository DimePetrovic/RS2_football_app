namespace Comeback.Match.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using FluentAssertions;
using Xunit;

public sealed class MatchApiTests : IClassFixture<MatchApiFactory>
{
    private readonly MatchApiFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public MatchApiTests(MatchApiFactory factory) => _factory = factory;

    private HttpClient ClientFor(Guid userId, string name = "Test Player", string role = "Player")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.For(userId, name, role));
        return client;
    }

    private static object CreateMatchBody(string title, string? location, DateTime startsAt,
        int playersPerTeam = 1, IEnumerable<object>? invitees = null)
        => new
        {
            title,
            type = "Independent",
            location,
            startsAt,
            durationMinutes = 60,
            playersPerTeam,
            maxSubstitutes = 3,
            invitees = invitees ?? [],
            groupId = (Guid?)null,
            opponentGroupId = (Guid?)null,
            guestNames = (string[]?)null,
        };

    private static async Task<Guid> CreatedIdAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task GetMatches_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/matches");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateMatch_ThenGet_ReturnsPersistedMatch()
    {
        var organizer = Guid.NewGuid();
        var client = ClientFor(organizer);

        var create = await client.PostAsJsonAsync("/api/matches",
            CreateMatchBody("Friday 8pm", "Altina", DateTime.UtcNow.AddDays(1)), Json);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var matchId = await CreatedIdAsync(create);

        var get = await client.GetAsync($"/api/matches/{matchId}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var match = await get.Content.ReadFromJsonAsync<JsonElement>(Json);
        match.GetProperty("title").GetString().Should().Be("Friday 8pm");
        match.GetProperty("location").GetString().Should().Be("Altina");
        match.GetProperty("organizerUserId").GetGuid().Should().Be(organizer);
        match.GetProperty("status").GetString().Should().Be("Scheduled");
    }

    [Fact]
    public async Task CreateMatch_WithoutLocation_IsRejectedByValidation()
    {
        var client = ClientFor(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/matches",
            CreateMatchBody("Bez lokacije", location: null, DateTime.UtcNow.AddDays(1)), Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AdminUser_CannotCreateMatch()
    {
        var client = ClientFor(Guid.NewGuid(), "Admin", role: "Admin");

        var response = await client.PostAsJsonAsync("/api/matches",
            CreateMatchBody("Admin match", "Field", DateTime.UtcNow.AddDays(1)), Json);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FullFlow_CreateAcceptAssignSubmitResult_UpdatesMatchAndStats()
    {
        var organizer = Guid.NewGuid();
        var opponent = Guid.NewGuid();
        var organizerClient = ClientFor(organizer);
        var opponentClient = ClientFor(opponent, "Opponent");

        // 1) Create a match that has already been "played" (start in the past), with one invited player.
        var create = await organizerClient.PostAsJsonAsync("/api/matches",
            CreateMatchBody("Match with result", "Field", DateTime.UtcNow.AddHours(-2),
                playersPerTeam: 1,
                invitees: [new { userId = opponent, displayName = "Opponent" }]), Json);
        var matchId = await CreatedIdAsync(create);

        // 2) The invited player accepts.
        (await opponentClient.PostAsJsonAsync($"/api/matches/{matchId}/respond", new { accept = true }, Json))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 3) The organizer assigns teams (self Home, opponent Away).
        (await organizerClient.PostAsJsonAsync($"/api/matches/{matchId}/teams/assign",
            new { targetUserId = organizer, team = "Home" }, Json))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await organizerClient.PostAsJsonAsync($"/api/matches/{matchId}/teams/assign",
            new { targetUserId = opponent, team = "Away" }, Json))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4) Unos rezultata 1:0 (gol organizatora).
        var submit = await organizerClient.PostAsJsonAsync($"/api/matches/{matchId}/result", new
        {
            homeScore = 1,
            awayScore = 0,
            goals = new[] { new { scorerUserId = organizer, isOwnGoal = false, assistUserId = (Guid?)null } },
        }, Json);
        submit.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5) The match now carries a result and ResultSubmitted status.
        var match = await (await organizerClient.GetAsync($"/api/matches/{matchId}"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        match.GetProperty("status").GetString().Should().Be("ResultSubmitted");
        match.GetProperty("homeScore").GetInt32().Should().Be(1);
        match.GetProperty("awayScore").GetInt32().Should().Be(0);
        match.GetProperty("myXpChange").GetInt32().Should().Be(MatchXpRules.WinXp);

        // 6) The organizer statistics reflect the played win.
        var stats = await (await organizerClient.GetAsync($"/api/matches/players/{organizer}/stats"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        stats.GetProperty("playedCount").GetInt32().Should().Be(1);
        stats.GetProperty("wins").GetInt32().Should().Be(1);
        stats.GetProperty("goals").GetInt32().Should().Be(1);

        // 7) An integration event was published for the Rating service.
        _factory.Events.Published.OfType<MatchResultSubmittedIntegrationEvent>()
            .Should().ContainSingle(e => e.MatchId == matchId);
    }

    [Fact]
    public async Task CancelMatch_SetsStatusToCancelled()
    {
        var organizer = Guid.NewGuid();
        var client = ClientFor(organizer);
        var matchId = await CreatedIdAsync(await client.PostAsJsonAsync("/api/matches",
            CreateMatchBody("To cancel", "Field", DateTime.UtcNow.AddDays(1)), Json));

        var cancel = await client.DeleteAsync($"/api/matches/{matchId}");
        cancel.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var match = await (await client.GetAsync($"/api/matches/{matchId}"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        match.GetProperty("status").GetString().Should().Be("Cancelled");
    }

    [Fact]
    public async Task UpdateMatchDetails_ChangesTitleAndLocation()
    {
        var organizer = Guid.NewGuid();
        var client = ClientFor(organizer);
        var matchId = await CreatedIdAsync(await client.PostAsJsonAsync("/api/matches",
            CreateMatchBody("Old title", "Old location", DateTime.UtcNow.AddDays(1)), Json));

        var update = await client.PutAsJsonAsync($"/api/matches/{matchId}", new
        {
            title = "Novi naziv",
            location = "New location",
            startsAt = DateTime.UtcNow.AddDays(2),
            durationMinutes = 90,
        }, Json);
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var match = await (await client.GetAsync($"/api/matches/{matchId}"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        match.GetProperty("title").GetString().Should().Be("Novi naziv");
        match.GetProperty("location").GetString().Should().Be("New location");
    }

    [Fact]
    public async Task CreateMatch_WithGuest_AddsGuestParticipantWithoutAccount()
    {
        var organizer = Guid.NewGuid();
        var client = ClientFor(organizer);
        var body = new
        {
            title = "Match with guest",
            type = "Independent",
            location = "Field",
            startsAt = DateTime.UtcNow.AddDays(1),
            durationMinutes = 60,
            playersPerTeam = 1,
            maxSubstitutes = 3,
            invitees = Array.Empty<object>(),
            groupId = (Guid?)null,
            opponentGroupId = (Guid?)null,
            guestNames = new[] { "Marko sa posla" },
        };

        var matchId = await CreatedIdAsync(await client.PostAsJsonAsync("/api/matches", body, Json));

        var match = await (await client.GetAsync($"/api/matches/{matchId}"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var guest = match.GetProperty("participants").EnumerateArray()
            .Single(p => p.GetProperty("isGuest").GetBoolean());

        guest.GetProperty("displayName").GetString().Should().Be("Marko sa posla");
        guest.GetProperty("status").GetString().Should().Be("Accepted");
        guest.GetProperty("username").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task RemoveParticipant_ByOrganizer_MarksParticipantRemoved()
    {
        var organizer = Guid.NewGuid();
        var player = Guid.NewGuid();
        var organizerClient = ClientFor(organizer);
        var playerClient = ClientFor(player, "Player");

        var matchId = await CreatedIdAsync(await organizerClient.PostAsJsonAsync("/api/matches",
            CreateMatchBody("Match with surplus", "Field", DateTime.UtcNow.AddDays(1),
                playersPerTeam: 1, invitees: [new { userId = player, displayName = "Player" }]), Json));

        (await playerClient.PostAsJsonAsync($"/api/matches/{matchId}/respond", new { accept = true }, Json))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var kick = await organizerClient.DeleteAsync($"/api/matches/{matchId}/participants/{player}");
        kick.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var match = await (await organizerClient.GetAsync($"/api/matches/{matchId}"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var kicked = match.GetProperty("participants").EnumerateArray()
            .Single(p => p.GetProperty("userId").GetGuid() == player);
        kicked.GetProperty("status").GetString().Should().Be("Removed");
    }

    [Fact]
    public async Task RemoveParticipant_ByNonOrganizer_IsForbidden()
    {
        var organizer = Guid.NewGuid();
        var player = Guid.NewGuid();
        var organizerClient = ClientFor(organizer);
        var playerClient = ClientFor(player, "Player");

        var matchId = await CreatedIdAsync(await organizerClient.PostAsJsonAsync("/api/matches",
            CreateMatchBody("Match", "Field", DateTime.UtcNow.AddDays(1),
                playersPerTeam: 1, invitees: [new { userId = player, displayName = "Player" }]), Json));

        // A non-organizer tries to kick the organizer -> forbidden.
        var kick = await playerClient.DeleteAsync($"/api/matches/{matchId}/participants/{organizer}");

        kick.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateMatch_SchedulesResultReminder_FifteenMinAfterEnd()
    {
        var organizer = Guid.NewGuid();
        var client = ClientFor(organizer);
        var startsAt = DateTime.UtcNow.AddDays(1);

        _factory.Scheduler.Scheduled.Clear();
        var matchId = await CreatedIdAsync(await client.PostAsJsonAsync("/api/matches",
            new
            {
                title = "Match",
                type = "Independent",
                location = "Field",
                startsAt,
                durationMinutes = 90,
                playersPerTeam = 5,
                maxSubstitutes = 3,
                invitees = Array.Empty<object>(),
                groupId = (Guid?)null,
                opponentGroupId = (Guid?)null,
                guestNames = (string[]?)null,
            }, Json));

        var job = _factory.Scheduler.Scheduled.Should().ContainSingle(j => j.MatchId == matchId).Subject;
        // Kraj (start + 90min) + 15min podsetnik.
        job.RunAt.Should().BeCloseTo(startsAt.AddMinutes(90 + 15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateMatchTime_CancelsOldReminderAndReschedules()
    {
        var organizer = Guid.NewGuid();
        var client = ClientFor(organizer);
        var matchId = await CreatedIdAsync(await client.PostAsJsonAsync("/api/matches",
            CreateMatchBody("Match", "Field", DateTime.UtcNow.AddDays(1)), Json));

        _factory.Scheduler.Cancelled.Clear();
        _factory.Scheduler.Scheduled.Clear();

        await client.PutAsJsonAsync($"/api/matches/{matchId}", new
        {
            title = "Match",
            location = "Field",
            startsAt = DateTime.UtcNow.AddDays(3),
            durationMinutes = 60,
        }, Json);

        _factory.Scheduler.Cancelled.Should().NotBeEmpty();
        _factory.Scheduler.Scheduled.Should().ContainSingle(j => j.MatchId == matchId);
    }

    [Fact]
    public async Task CancelMatch_CancelsResultReminder()
    {
        var organizer = Guid.NewGuid();
        var client = ClientFor(organizer);
        var matchId = await CreatedIdAsync(await client.PostAsJsonAsync("/api/matches",
            CreateMatchBody("To cancel", "Field", DateTime.UtcNow.AddDays(1)), Json));

        _factory.Scheduler.Cancelled.Clear();
        await client.DeleteAsync($"/api/matches/{matchId}");

        _factory.Scheduler.Cancelled.Should().NotBeEmpty();
    }

    [Fact]
    public async Task InternalEndpoint_IsServedWithoutAuth_SoMustBeGatewayBlocked()
    {
        // The internal endpoint intentionally has no auth (another service calls it internally).
        // This test locks that contract in — external protection is the gateway's responsibility.
        var organizer = Guid.NewGuid();
        var client = ClientFor(organizer);
        var create = await client.PostAsJsonAsync("/api/matches",
            CreateMatchBody("Internal", "Field", DateTime.UtcNow.AddDays(1)), Json);
        var matchId = await CreatedIdAsync(create);

        var noAuth = _factory.CreateClient();
        var response = await noAuth.GetAsync($"/api/matches/internal/{matchId}/details");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
