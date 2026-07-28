namespace Comeback.DemoSeeder;

using System.Text.Json;

/// <summary>
/// Seed tece kroz javne API tokove (gateway), pa sve nizvodno — profili, XP, feed postovi,
/// fan-out pratiocima — nastaje kroz prave evente, isto kao u produkcijskoj upotrebi.
/// Svaka faza je idempotentna, pa je ponovni run bezbedan i brz.
/// </summary>
public sealed class Seeder
{
    private readonly ApiClient _api;
    private readonly Dictionary<int, ApiClient.SeededUser> _users = [];
    private readonly Dictionary<int, Guid> _groupIds = [];

    /// <summary>Odigrani mecevi kojima u ovom runu treba uneti/proveriti feed interakcije.</summary>
    private readonly List<(DemoMatch Match, Guid MatchId, int[] ParticipantIxs)> _playedMatches = [];

    public Seeder(ApiClient api) => _api = api;

    private static DemoUser U(int ix) => DemoData.Users[ix - 1];

    private ApiClient.SeededUser S(int ix) => _users[ix];

    private static void Banner(string text) => Console.WriteLine($"{Environment.NewLine}=== {text} ===");

    private static void Line(string item, string status) => Console.WriteLine($"  {item,-45} {status}");

    public async Task RunAsync()
    {
        await SeedUsersAsync();
        await WaitForProfilesAsync();
        await SeedFollowsAsync();
        await SeedGroupsAsync();
        await SeedMatchesAsync();
        await SeedFeedInteractionsAsync();
        await SeedReviewsAsync();
    }

    // ── Faza 1: korisnici ────────────────────────────────────────────────

    private async Task SeedUsersAsync()
    {
        Banner($"[1/7] Korisnici ({DemoData.Users.Count})");
        for (var ix = 1; ix <= DemoData.Users.Count; ix++)
        {
            var user = U(ix);
            var seeded = await _api.EnsureUserAsync(user);
            _users[ix] = seeded;
            Line(user.Email, seeded.Created ? "OK (registrovan)" : "SKIP (postoji)");
        }
    }

    // ── Faza 2: profili (auth → profile event) ───────────────────────────

    private async Task WaitForProfilesAsync()
    {
        Banner("[2/7] Cekanje profila (event auth → profile)");
        for (var ix = 1; ix <= DemoData.Users.Count; ix++)
        {
            await _api.WaitForProfileAsync(S(ix).Token, U(ix).Email);
        }

        Console.WriteLine("  Svi profili su dostupni.");
    }

    // ── Faza 3: follow graf ──────────────────────────────────────────────

    private async Task SeedFollowsAsync()
    {
        Banner($"[3/7] Follow veze ({DemoData.Follows.Count})");
        var ok = 0;
        foreach (var (followerIx, followedIx) in DemoData.Follows)
        {
            // Server je idempotentan (postojeci follow → 204), pa nema posebne provere.
            using var response = await _api.PostAsync(
                $"/api/profiles/{S(followedIx).UserId}/follow", body: null, S(followerIx).Token);
            if (response.IsSuccessStatusCode)
            {
                ok++;
            }
            else
            {
                Line($"{U(followerIx).Username} → {U(followedIx).Username}", $"WARN ({(int)response.StatusCode})");
            }
        }

        Console.WriteLine($"  {ok}/{DemoData.Follows.Count} veza potvrdjeno.");
    }

    // ── Faza 4: grupe ────────────────────────────────────────────────────

    private async Task SeedGroupsAsync()
    {
        Banner($"[4/7] Grupe ({DemoData.Groups.Count})");
        for (var gix = 1; gix <= DemoData.Groups.Count; gix++)
        {
            var group = DemoData.Groups[gix - 1];
            var captainToken = S(group.CaptainIx).Token;

            var mine = await _api.GetJsonAsync("/api/groups/mine", captainToken);
            var existing = mine?.EnumerateArray()
                .Where(g => g.GetProperty("name").GetString() == group.Name)
                .Select(g => (Guid?)g.GetProperty("id").GetGuid())
                .FirstOrDefault();
            if (existing is not null)
            {
                _groupIds[gix] = existing.Value;
                Line(group.Name, "SKIP (postoji)");
                continue;
            }

            using var create = await _api.PostAsync("/api/groups", new
            {
                name = group.Name,
                avatarUrl = (string?)null,
                memberUserIds = group.MemberIxs.Select(ix => S(ix).UserId).ToArray(),
            }, captainToken);

            if (!create.IsSuccessStatusCode)
                throw new InvalidOperationException($"Grupa \"{group.Name}\" nije kreirana ({(int)create.StatusCode}).");

            var body = await ApiClient.ReadJsonAsync(create);
            _groupIds[gix] = body.GetProperty("id").GetGuid();
            Line(group.Name, $"OK ({group.MemberIxs.Length + 1} clanova)");
        }
    }

    // ── Faza 5: mecevi ───────────────────────────────────────────────────

    private async Task SeedMatchesAsync()
    {
        Banner($"[5/7] Mecevi ({DemoData.Matches.Count})");
        foreach (var match in DemoData.Matches)
        {
            var organizer = S(match.OrganizerIx);
            var existing = await FindMatchByTitleAsync(match.Title, organizer.Token);

            if (existing is { } found && (!match.IsPlayed || found.Status == "ResultSubmitted"))
            {
                Line(match.Title, "SKIP (postoji)");
                if (match.IsPlayed)
                {
                    _playedMatches.Add((match, found.Id, ParticipantIxs(match)));
                }

                continue;
            }

            Guid matchId;
            var createdNow = false;
            if (existing is { } scheduled)
            {
                matchId = scheduled.Id; // nedovrsen raniji run — nastavi od prihvatanja poziva
            }
            else
            {
                matchId = await CreateMatchAsync(match, organizer.Token);
                createdNow = true;
            }

            if (match.IsPlayed)
            {
                await DriveMatchToResultAsync(match, matchId, organizer);
                _playedMatches.Add((match, matchId, ParticipantIxs(match)));
                Line(match.Title, $"OK ({match.HomeScore}:{match.AwayScore})");
            }
            else
            {
                if (createdNow && match.RequestPlayers)
                {
                    using var request = await _api.PostAsync(
                        $"/api/matches/{matchId}/request-players",
                        new { position = match.RequestPosition }, organizer.Token);
                    Line(match.Title, request.IsSuccessStatusCode
                        ? "OK (+ trazimo igraca)"
                        : $"WARN (request-players {(int)request.StatusCode})");
                }
                else
                {
                    Line(match.Title, "OK (predstojeci)");
                }
            }
        }
    }

    private async Task<(Guid Id, string Status)?> FindMatchByTitleAsync(string title, string token)
    {
        var list = await _api.GetJsonAsync("/api/matches/", token);
        if (list is null) return null;

        foreach (var item in list.Value.EnumerateArray())
        {
            if (item.GetProperty("title").GetString() == title)
                return (item.GetProperty("id").GetGuid(), item.GetProperty("status").GetString()!);
        }

        return null;
    }

    private async Task<Guid> CreateMatchAsync(DemoMatch match, string organizerToken)
    {
        using var create = await _api.PostAsync("/api/matches", new
        {
            title = match.Title,
            type = match.Type,
            location = match.Location,
            startsAt = DateTime.UtcNow.AddDays(match.DaysOffset),
            durationMinutes = 90,
            playersPerTeam = match.PlayersPerTeam,
            maxSubstitutes = 2,
            invitees = match.InviteeIxs
                .Select(ix => new { userId = S(ix).UserId, displayName = U(ix).DisplayName })
                .ToArray(),
            groupId = match.GroupIx is { } g ? _groupIds[g] : (Guid?)null,
            opponentGroupId = match.OpponentGroupIx is { } og ? _groupIds[og] : (Guid?)null,
            guestNames = (string[]?)null,
        }, organizerToken);

        if (!create.IsSuccessStatusCode)
            throw new InvalidOperationException($"Mec \"{match.Title}\" nije kreiran ({(int)create.StatusCode}).");

        var body = await ApiClient.ReadJsonAsync(create);
        return body.GetProperty("id").GetGuid();
    }

    /// <summary>Svi demo ucesnici meca (ukljucujuci organizatora), kao 1-bazirani indeksi.</summary>
    private static int[] ParticipantIxs(DemoMatch match)
    {
        IEnumerable<int> ixs = match.Type switch
        {
            "GroupMatch" => DemoData.Groups[match.GroupIx!.Value - 1].AllIxs,
            "GroupVsGroup" => DemoData.Groups[match.GroupIx!.Value - 1].AllIxs
                .Concat(DemoData.Groups[match.OpponentGroupIx!.Value - 1].AllIxs),
            _ => match.InviteeIxs,
        };
        return ixs.Concat([match.OrganizerIx]).Distinct().ToArray();
    }

    private async Task DriveMatchToResultAsync(DemoMatch match, Guid matchId, ApiClient.SeededUser organizer)
    {
        // GroupVsGroup: protivnicki kapiten prvo prihvata grupni poziv — tek to poziva njegove clanove.
        if (match.Type == "GroupVsGroup")
        {
            var opponentCaptain = S(DemoData.Groups[match.OpponentGroupIx!.Value - 1].CaptainIx);
            using var groupInvite = await _api.PostAsync(
                $"/api/matches/{matchId}/group-invite/respond", new { accept = true }, opponentCaptain.Token);
            // 400 = vec prihvaceno u ranijem runu; sve ostalo je stvarna greska.
        }

        foreach (var ix in ParticipantIxs(match).Where(ix => ix != match.OrganizerIx))
        {
            // Toleirisemo 400 (poziv vec prihvacen u ranijem runu).
            using var respond = await _api.PostAsync(
                $"/api/matches/{matchId}/respond", new { accept = true }, S(ix).Token);
        }

        if (match.Type == "GroupVsGroup")
        {
            // Clanovi su prihvatanjem automatski rasporedjeni na stranu svoje grupe;
            // jedino organizator (kapiten domacina) nema tim, pa ga rucno saljemo u Home.
            using var assign = await _api.PostAsync(
                $"/api/matches/{matchId}/teams/assign",
                new { targetUserId = organizer.UserId, team = "Home" }, organizer.Token);
        }
        else
        {
            using var randomize = await _api.PostAsync(
                $"/api/matches/{matchId}/teams/randomize", body: null, organizer.Token);
            if (!randomize.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Randomizacija timova za \"{match.Title}\" nije prosla ({(int)randomize.StatusCode}).");
        }

        var detail = await _api.GetJsonAsync($"/api/matches/{matchId}", organizer.Token)
            ?? throw new InvalidOperationException($"Detalj meca \"{match.Title}\" nije dostupan.");

        var home = TeamRoster(detail, "Home");
        var away = TeamRoster(detail, "Away");

        using var result = await _api.PostAsync($"/api/matches/{matchId}/result", new
        {
            homeScore = match.HomeScore,
            awayScore = match.AwayScore,
            goals = BuildGoals(match, home, away),
        }, organizer.Token);

        if (!result.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Unos rezultata za \"{match.Title}\" nije prosao ({(int)result.StatusCode}).");
    }

    private static List<Guid> TeamRoster(JsonElement detail, string team)
        => detail.GetProperty("participants").EnumerateArray()
            .Where(p => p.GetProperty("status").GetString() == "Accepted"
                     && p.GetProperty("team").GetString() == team)
            .Select(p => p.GetProperty("userId").GetGuid())
            .ToList();

    /// <summary>
    /// Golovi po strani moraju tacno da prate rezultat; autogol se pise igracu suprotnog tima.
    /// "Zvezde" iz dataseta dobijaju prednost kada su u sastavu, da tabele strelaca budu zive.
    /// </summary>
    private object[] BuildGoals(DemoMatch match, List<Guid> home, List<Guid> away)
    {
        var goals = new List<object>();
        AddGoalsForSide(goals, match.HomeScore, home, away, match.WithOwnGoal);
        AddGoalsForSide(goals, match.AwayScore, away, home, withOwnGoal: false);
        return [.. goals];
    }

    private void AddGoalsForSide(
        List<object> goals, int count, List<Guid> scoringTeam, List<Guid> opposingTeam, bool withOwnGoal)
    {
        var stars = DemoData.StarScorerIxs
            .Select(ix => S(ix).UserId)
            .Where(scoringTeam.Contains)
            .ToList();

        for (var i = 0; i < count; i++)
        {
            if (withOwnGoal && i == 0 && opposingTeam.Count > 0)
            {
                goals.Add(new
                {
                    scorerUserId = opposingTeam[^1],
                    isOwnGoal = true,
                    assistUserId = (Guid?)null,
                });
                continue;
            }

            var scorer = stars.Count > 0 && i < stars.Count * 2
                ? stars[i % stars.Count]
                : scoringTeam[i % scoringTeam.Count];

            var assist = i % 2 == 1
                ? scoringTeam.FirstOrDefault(id => id != scorer)
                : Guid.Empty;

            goals.Add(new
            {
                scorerUserId = scorer,
                isOwnGoal = false,
                assistUserId = assist == Guid.Empty ? (Guid?)null : assist,
            });
        }
    }

    // ── Faza 6: feed (lajkovi i komentari) ───────────────────────────────

    private async Task SeedFeedInteractionsAsync()
    {
        Banner($"[6/7] Feed interakcije ({_playedMatches.Count} postova)");
        for (var mi = 0; mi < _playedMatches.Count; mi++)
        {
            var (match, matchId, participantIxs) = _playedMatches[mi];
            try
            {
                var organizerToken = S(match.OrganizerIx).Token;

                // Post nastaje asinhrono (match → social preko RabbitMQ-a + fan-out), pa cekamo.
                var postId = await ApiClient.WaitForAsync(
                    () => FindPostIdAsync(matchId, organizerToken),
                    $"feed post za \"{match.Title}\"",
                    timeoutSeconds: 90);

                var likerIxs = participantIxs.Take(3 + (mi % 4)).ToArray();
                var likes = 0;
                foreach (var ix in likerIxs)
                {
                    if (await EnsureLikeAsync(postId, S(ix).Token)) likes++;
                }

                var comments = 0;
                for (var j = 0; j <= mi % 3; j++)
                {
                    var authorIx = participantIxs[(mi + j * 2) % participantIxs.Length];
                    var content = DemoData.Comments[(mi + j) % DemoData.Comments.Count];
                    if (await EnsureCommentAsync(postId, S(authorIx), content)) comments++;
                }

                Line(match.Title, $"OK ({likes} lajkova, {comments} komentara)");
            }
            catch (Exception ex)
            {
                Line(match.Title, $"WARN ({ex.Message})");
            }
        }
    }

    private async Task<string?> FindPostIdAsync(Guid matchId, string token)
    {
        var feed = await _api.GetJsonAsync("/api/feed?page=0&pageSize=50", token);
        if (feed is null) return null;

        foreach (var post in feed.Value.EnumerateArray())
        {
            if (post.TryGetProperty("matchId", out var mid) && mid.ValueKind == JsonValueKind.String
                && mid.GetGuid() == matchId)
            {
                return post.GetProperty("id").GetGuid().ToString();
            }
        }

        return null;
    }

    /// <summary>Reakcija je toggle — lajkujemo samo ako korisnik post jos nije lajkovao.</summary>
    private async Task<bool> EnsureLikeAsync(string postId, string token)
    {
        var post = await _api.GetJsonAsync($"/api/posts/{postId}", token);
        if (post is null) return false;
        if (post.Value.GetProperty("likedByMe").GetBoolean()) return true;

        using var like = await _api.PostAsync($"/api/posts/{postId}/reactions", body: null, token);
        return like.IsSuccessStatusCode;
    }

    private async Task<bool> EnsureCommentAsync(string postId, ApiClient.SeededUser author, string content)
    {
        var existing = await _api.GetJsonAsync($"/api/posts/{postId}/comments", author.Token);
        if (existing is not null)
        {
            foreach (var comment in existing.Value.EnumerateArray())
            {
                if (comment.GetProperty("authorUserId").GetGuid() == author.UserId
                    && comment.GetProperty("content").GetString() == content)
                {
                    return true;
                }
            }
        }

        using var post = await _api.PostAsync($"/api/posts/{postId}/comments", new { content }, author.Token);
        return post.IsSuccessStatusCode;
    }

    // ── Faza 7: recenzije ────────────────────────────────────────────────

    /// <summary>Par recenzija na dva najskorija odigrana meca — dovoljno da profili deluju zivo.</summary>
    private async Task SeedReviewsAsync()
    {
        Banner("[7/7] Recenzije (2 najskorija meca)");
        foreach (var (match, matchId, _) in _playedMatches.TakeLast(2))
        {
            try
            {
                var organizerToken = S(match.OrganizerIx).Token;
                var detail = await _api.GetJsonAsync($"/api/matches/{matchId}", organizerToken);
                if (detail is null)
                {
                    Line(match.Title, "WARN (detalj nedostupan)");
                    continue;
                }

                // (userId, participantId) za ucesnike koji su prihvatili i dobili tim.
                var eligible = detail.Value.GetProperty("participants").EnumerateArray()
                    .Where(p => p.GetProperty("status").GetString() == "Accepted"
                             && p.GetProperty("team").GetString() != "None")
                    .Select(p => (UserId: p.GetProperty("userId").GetGuid(),
                                  ParticipantId: p.GetProperty("id").GetGuid()))
                    .ToList();

                var byUserId = _users.ToDictionary(u => u.Value.UserId, u => u.Value);
                var submitted = 0;
                for (var j = 0; j < Math.Min(3, eligible.Count - 1); j++)
                {
                    var reviewer = eligible[j];
                    var reviewed = eligible[(j + 1) % eligible.Count];
                    if (!byUserId.TryGetValue(reviewer.UserId, out var reviewerUser)) continue;

                    using var review = await _api.PostAsync($"/api/matches/{matchId}/reviews", new
                    {
                        reviewedParticipantId = reviewed.ParticipantId,
                        overallRating = 7.0m + (j * 0.5m),
                        goalkeepingRating = (decimal?)null,
                        defenseRating = (decimal?)null,
                        attackRating = (decimal?)null,
                        effortRating = 8.0m,
                        comment = (string?)null,
                    }, reviewerUser.Token);

                    if (review.IsSuccessStatusCode) submitted++;
                }

                Line(match.Title, $"OK ({submitted} recenzija)");
            }
            catch (Exception ex)
            {
                Line(match.Title, $"WARN ({ex.Message})");
            }
        }
    }
}
