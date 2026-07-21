namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

/// <summary>
/// The single source of truth for XP awarded after a match. Used by the Rating service (awarding)
/// and the Match service (summary dialog display), so the values never diverge.
/// </summary>
public static class MatchXpRules
{
    public const int WinXp = 150;
    public const int DrawXp = 50;
    public const int LossXp = 25;
    public const int CaptainBonus = 30;

    public static int Calculate(bool isWinner, bool isDraw, bool isCaptain)
    {
        var xp = isDraw ? DrawXp : isWinner ? WinXp : LossXp;
        if (isCaptain) xp += CaptainBonus;
        return xp;
    }
}
