namespace Comeback.Rating.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;
using Comeback.Rating.Domain.Events;

public sealed class PlayerXp : AggregateRoot<Guid>
{
    private const int YouthXpPerSeason = 1_000;
    private const int SeniorXpPerSeason = 2_500;

    public Guid UserId { get; private set; }
    public int CareerXp { get; private set; }
    public int MatchXp { get; private set; }
    public int TotalXp => CareerXp + MatchXp;
    public int Level => CalculateLevel(TotalXp);
    public int YouthSeasons { get; private set; }
    public int SeniorSeasons { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PlayerXp() { }

    private PlayerXp(Guid id, Guid userId, int youthSeasons, int seniorSeasons) : base(id)
    {
        UserId = userId;
        YouthSeasons = youthSeasons;
        SeniorSeasons = seniorSeasons;
        CareerXp = ComputeCareerXp(youthSeasons, seniorSeasons);
        MatchXp = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static PlayerXp Create(Guid userId, int youthSeasons, int seniorSeasons)
    {
        var xp = new PlayerXp(Guid.NewGuid(), userId, youthSeasons, seniorSeasons);
        xp.RaiseDomainEvent(new PlayerXpUpdatedDomainEvent(userId, xp.TotalXp, xp.Level));
        return xp;
    }

    public void UpdateCareerXp(int youthSeasons, int seniorSeasons)
    {
        YouthSeasons = youthSeasons;
        SeniorSeasons = seniorSeasons;
        CareerXp = ComputeCareerXp(youthSeasons, seniorSeasons);
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new PlayerXpUpdatedDomainEvent(UserId, TotalXp, Level));
    }

    public void AddMatchXp(int amount)
    {
        MatchXp += amount;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new PlayerXpUpdatedDomainEvent(UserId, TotalXp, Level));
    }

    // XP for level N = 400 × (N-1)²  ->  Level = floor(1 + sqrt(TotalXp / 400))
    public static int CalculateLevel(int totalXp)
    {
        if (totalXp <= 0) return 1;
        return (int)Math.Floor(1 + Math.Sqrt(totalXp / 400.0));
    }

    private static int ComputeCareerXp(int youthSeasons, int seniorSeasons)
        => youthSeasons * YouthXpPerSeason + seniorSeasons * SeniorXpPerSeason;
}
