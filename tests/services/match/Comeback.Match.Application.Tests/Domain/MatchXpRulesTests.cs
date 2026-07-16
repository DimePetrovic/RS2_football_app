namespace Comeback.Match.Application.Tests.Domain;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using FluentAssertions;
using Xunit;

public sealed class MatchXpRulesTests
{
    [Fact]
    public void Calculate_Win_GivesWinXp()
        => MatchXpRules.Calculate(isWinner: true, isDraw: false, isCaptain: false)
            .Should().Be(MatchXpRules.WinXp);

    [Fact]
    public void Calculate_Draw_GivesDrawXp()
        => MatchXpRules.Calculate(isWinner: false, isDraw: true, isCaptain: false)
            .Should().Be(MatchXpRules.DrawXp);

    [Fact]
    public void Calculate_Loss_GivesLossXp()
        => MatchXpRules.Calculate(isWinner: false, isDraw: false, isCaptain: false)
            .Should().Be(MatchXpRules.LossXp);

    [Fact]
    public void Calculate_Captain_AddsBonusOnTopOfBase()
    {
        var withoutBonus = MatchXpRules.Calculate(isWinner: true, isDraw: false, isCaptain: false);
        var withBonus = MatchXpRules.Calculate(isWinner: true, isDraw: false, isCaptain: true);

        withBonus.Should().Be(withoutBonus + MatchXpRules.CaptainBonus);
    }

    [Fact]
    public void Calculate_CaptainBonus_AppliesRegardlessOfOutcome()
    {
        MatchXpRules.Calculate(false, true, isCaptain: true)
            .Should().Be(MatchXpRules.DrawXp + MatchXpRules.CaptainBonus);
        MatchXpRules.Calculate(false, false, isCaptain: true)
            .Should().Be(MatchXpRules.LossXp + MatchXpRules.CaptainBonus);
    }
}
