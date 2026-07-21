namespace Comeback.Rating.Application.Tests.Domain;

using Comeback.Rating.Domain.Entities;
using FluentAssertions;
using Xunit;

public sealed class PlayerXpTests
{
    [Theory]
    [InlineData(0, 1)]      // no XP -> level 1
    [InlineData(399, 1)]    // tik ispod praga za nivo 2
    [InlineData(400, 2)]    // 400×(2-1)² = 400
    [InlineData(1599, 2)]   // tik ispod nivoa 3
    [InlineData(1600, 3)]   // 400×(3-1)² = 1600
    [InlineData(3600, 4)]   // 400×(4-1)² = 3600
    public void CalculateLevel_FollowsQuadraticThresholds(int totalXp, int expectedLevel)
    {
        PlayerXp.CalculateLevel(totalXp).Should().Be(expectedLevel);
    }

    [Fact]
    public void CalculateLevel_NegativeXp_ClampsToLevelOne()
    {
        PlayerXp.CalculateLevel(-100).Should().Be(1);
    }

    [Fact]
    public void Create_ComputesCareerXpFromSeasons()
    {
        // 2 omladinske × 1000 + 3 seniorske × 2500 = 9500
        var xp = PlayerXp.Create(Guid.NewGuid(), youthSeasons: 2, seniorSeasons: 3);

        xp.CareerXp.Should().Be(9500);
        xp.MatchXp.Should().Be(0);
        xp.TotalXp.Should().Be(9500);
    }

    [Fact]
    public void AddMatchXp_AccumulatesOntoTotal()
    {
        var xp = PlayerXp.Create(Guid.NewGuid(), 0, 0);

        xp.AddMatchXp(150);
        xp.AddMatchXp(50);

        xp.MatchXp.Should().Be(200);
        xp.TotalXp.Should().Be(200);
    }

    [Fact]
    public void UpdateCareerXp_RecomputesCareerButKeepsMatchXp()
    {
        var xp = PlayerXp.Create(Guid.NewGuid(), 0, 0);
        xp.AddMatchXp(300);

        xp.UpdateCareerXp(youthSeasons: 1, seniorSeasons: 0);

        xp.CareerXp.Should().Be(1000);
        xp.MatchXp.Should().Be(300);
        xp.TotalXp.Should().Be(1300);
    }
}
