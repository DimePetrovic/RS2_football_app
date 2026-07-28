namespace Comeback.DemoSeeder;

/// <summary>
/// Demo korisnik. Indeksi u ostatku dataseta su 1-bazirani redni brojevi u <see cref="DemoData.Users"/>.
/// Pozicije prate enum iz Auth servisa: Goalkeeper=0, Defender=1, Midfielder=2, Forward=3.
/// </summary>
public sealed record DemoUser(
    string FirstName,
    string LastName,
    string Username,
    int PreferredPosition,
    bool CanPlayGoalkeeper,
    string DateOfBirth,
    int YouthSeasons,
    int SeniorSeasons)
{
    public string Email => $"{Username}@demo.comeback.com";

    public string DisplayName => $"{FirstName} {LastName}";
}

public sealed record DemoGroup(string Name, int CaptainIx, int[] MemberIxs)
{
    /// <summary>Svi clanovi grupe, ukljucujuci kapitena.</summary>
    public IEnumerable<int> AllIxs => new[] { CaptainIx }.Concat(MemberIxs);
}

/// <summary>
/// Demo mec. Odigrani mecevi imaju negativan <paramref name="DaysOffset"/> i rezultat;
/// predstojeci imaju pozitivan offset i (opciono) "trazimo igraca" poziv.
/// Za GroupMatch/GroupVsGroup ucesnici dolaze iz grupa, pa je <paramref name="InviteeIxs"/> prazan.
/// </summary>
public sealed record DemoMatch(
    string Title,
    string Type,
    int DaysOffset,
    string Location,
    int PlayersPerTeam,
    int OrganizerIx,
    int[] InviteeIxs,
    int? GroupIx = null,
    int? OpponentGroupIx = null,
    int HomeScore = 0,
    int AwayScore = 0,
    bool WithOwnGoal = false,
    bool RequestPlayers = false,
    string? RequestPosition = null)
{
    public bool IsPlayed => DaysOffset < 0;
}

/// <summary>Staticki dataset — bez logike. Sve lozinke su Test!234.</summary>
public static class DemoData
{
    public const string Password = "Test!234";

    public static readonly IReadOnlyList<DemoUser> Users =
    [
        new("Marko", "Petrović", "marko.petrovic", 3, false, "1994-03-12", 4, 6),
        new("Nikola", "Jovanović", "nikola.jovanovic", 2, false, "1991-07-25", 3, 8),
        new("Luka", "Đorđević", "luka.djordjevic", 1, false, "1997-01-08", 2, 4),
        new("Stefan", "Nikolić", "stefan.nikolic", 0, true, "1993-11-30", 5, 7),
        new("Miloš", "Stojanović", "milos.stojanovic", 2, false, "1999-05-17", 1, 3),
        new("Aleksandar", "Ilić", "aleksandar.ilic", 1, false, "1990-09-02", 6, 10),
        new("Nemanja", "Pavlović", "nemanja.pavlovic", 3, false, "1996-12-21", 3, 5),
        new("Dušan", "Kovačević", "dusan.kovacevic", 1, false, "1992-04-14", 2, 9),
        new("Filip", "Milošević", "filip.milosevic", 2, false, "2001-08-06", 2, 2),
        new("Vladimir", "Petković", "vladimir.petkovic", 1, false, "1995-02-28", 4, 5),
        new("Jovan", "Simić", "jovan.simic", 0, true, "1998-06-11", 3, 4),
        new("Uroš", "Radovanović", "uros.radovanovic", 3, false, "2000-10-19", 1, 2),
        new("Milan", "Todorović", "milan.todorovic", 2, false, "1994-08-23", 5, 6),
        new("Petar", "Živković", "petar.zivkovic", 1, false, "2002-03-05", 1, 1),
        new("Lazar", "Marinković", "lazar.marinkovic", 3, false, "1997-07-15", 2, 5),
        new("Ognjen", "Vasić", "ognjen.vasic", 2, false, "2003-01-27", 1, 1),
    ];

    /// <summary>G1 i G2 su disjunktne — uslov za GroupVsGroup mec.</summary>
    public static readonly IReadOnlyList<DemoGroup> Groups =
    [
        new("FK Blokovi", CaptainIx: 1, MemberIxs: [2, 4, 5, 7, 9, 12]),
        new("Ada Veterani", CaptainIx: 8, MemberIxs: [3, 6, 10, 11, 14]),
        new("Ponedeljak u Košutnjaku", CaptainIx: 2, MemberIxs: [1, 5, 13, 15, 16]),
    ];

    /// <summary>(pratilac, praceni) — clanovi prate kapitene, korisnici 1 i 2 su "zvezde".</summary>
    public static readonly IReadOnlyList<(int FollowerIx, int FollowedIx)> Follows =
    [
        // FK Blokovi prati kapitena Marka (1).
        (2, 1), (4, 1), (5, 1), (7, 1), (9, 1), (12, 1),
        // Ada Veterani prati kapitena Dusana (8).
        (3, 8), (6, 8), (10, 8), (11, 8), (14, 8),
        // Kosutnjak prati kapitena Nikolu (2).
        (1, 2), (5, 2), (13, 2), (15, 2), (16, 2),
        // Marko (1) i Nikola (2) su zvezde — prate ih i van grupa.
        (3, 1), (6, 1), (8, 1), (10, 1), (13, 1), (15, 1),
        (4, 2), (7, 2), (9, 2), (11, 2), (14, 2),
        // Uzajamni parovi i malo organskog haosa.
        (1, 7), (7, 12), (12, 7), (3, 4), (8, 3), (10, 6),
        (13, 5), (15, 12), (16, 9), (11, 4), (14, 10), (6, 3),
    ];

    /// <summary>Napadaci koji "vuku" statistiku strelaca kada se nadju u sastavu.</summary>
    public static readonly IReadOnlyList<int> StarScorerIxs = [1, 7, 12, 15];

    /// <summary>Naslov meca je idempotency kljuc — ne menjati bez razloga.</summary>
    public static readonly IReadOnlyList<DemoMatch> Matches =
    [
        new("Subotnji fudbal na Adi", "Independent", -35, "Ada Ciganlija — teren 3", 4,
            OrganizerIx: 1, InviteeIxs: [2, 3, 4, 5, 6, 7, 8, 9, 10], HomeScore: 5, AwayScore: 3),
        new("FK Blokovi — trening meč", "GroupMatch", -28, "Blokovi — balon", 3,
            OrganizerIx: 1, InviteeIxs: [], GroupIx: 1, HomeScore: 2, AwayScore: 2),
        new("Blokovi vs Ada Veterani", "GroupVsGroup", -21, "SC Banjica", 5,
            OrganizerIx: 1, InviteeIxs: [], GroupIx: 1, OpponentGroupIx: 2, HomeScore: 4, AwayScore: 1),
        new("Fudbal sredom — Banjica", "Independent", -14, "SC Banjica", 4,
            OrganizerIx: 6, InviteeIxs: [3, 4, 8, 10, 11, 13, 14, 15, 16], HomeScore: 3, AwayScore: 2),
        new("Ada Veterani — interni meč", "GroupMatch", -10, "Košutnjak", 2,
            OrganizerIx: 8, InviteeIxs: [], GroupIx: 2, HomeScore: 1, AwayScore: 0),
        new("Revanš: Ada Veterani vs Blokovi", "GroupVsGroup", -5, "SC Banjica", 5,
            OrganizerIx: 8, InviteeIxs: [], GroupIx: 2, OpponentGroupIx: 1,
            HomeScore: 6, AwayScore: 4, WithOwnGoal: true),
        new("Petak uveče — Hala sportova", "Independent", -2, "Hala sportova Ranko Žeravica", 4,
            OrganizerIx: 2, InviteeIxs: [1, 5, 7, 9, 12, 13, 15, 16], HomeScore: 2, AwayScore: 1),
        new("Nedeljna liga — treba nam vezni", "Independent", 3, "Ada Ciganlija — teren 1", 5,
            OrganizerIx: 1, InviteeIxs: [2, 5, 7], RequestPlayers: true, RequestPosition: "Midfielder"),
        new("Prijateljski meč — Zvezdara", "Independent", 6, "SC Olimp — Zvezdara", 5,
            OrganizerIx: 8, InviteeIxs: [3, 11, 14], RequestPlayers: true, RequestPosition: null),
    ];

    public static readonly IReadOnlyList<string> Comments =
    [
        "Kakva utakmica, bravo ekipo!",
        "Svaka čast za golove, majstore",
        "Revanš sledeće nedelje?",
        "Golman nas je spasao danas",
        "Odbrana je pukla u drugom poluvremenu",
        "Ajmo isti sastav i sledeći put",
        "Ko snima sledeći meč?",
    ];
}
