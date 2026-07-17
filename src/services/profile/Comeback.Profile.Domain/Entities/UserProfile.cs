namespace Comeback.Profile.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;
using Comeback.Profile.Domain.Enums;
using Comeback.Profile.Domain.Events;
using Comeback.BuildingBlocks.Domain.Constants;

public sealed class UserProfile : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public Position PreferredPosition { get; private set; }
    public bool CanPlayGoalkeeper { get; private set; }
    public int YouthSeasons { get; private set; }
    public int SeniorSeasons { get; private set; }
    public string? DisplayName { get; private set; }

    // ISO 3166-1 alpha-2 country code; null when the player did not pick one.
    public string? Nationality { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public SkillLevel? SkillLevel { get; private set; }
    public string Role { get; private set; } = UserRoles.Player;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserProfile() { }

    private UserProfile(
        Guid id,
        Guid userId,
        string username,
        string email,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        Position preferredPosition,
        bool canPlayGoalkeeper,
        int youthSeasons,
        int seniorSeasons,
        string role) : base(id)
    {
        UserId = userId;
        Username = username;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        PreferredPosition = preferredPosition;
        CanPlayGoalkeeper = preferredPosition == Position.Goalkeeper || canPlayGoalkeeper;
        YouthSeasons = youthSeasons;
        SeniorSeasons = seniorSeasons;
        DisplayName = $"{firstName} {lastName}";
        Role = role;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static UserProfile Create(
        Guid userId,
        string username,
        string email,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        Position preferredPosition,
        bool canPlayGoalkeeper,
        int youthSeasons,
        int seniorSeasons,
        string role,
        string? nationality = null)
    {
        var profile = new UserProfile(Guid.NewGuid(), userId, username, email, firstName, lastName, dateOfBirth, preferredPosition, canPlayGoalkeeper, youthSeasons, seniorSeasons, role)
        {
            Nationality = nationality,
        };
        profile.RaiseDomainEvent(new ProfileCreatedDomainEvent(userId, profile.Id));
        return profile;
    }

    public void Update(
        string? displayName,
        string? bio,
        string? avatarUrl,
        Position? preferredPosition,
        bool? canPlayGoalkeeper,
        SkillLevel? skillLevel,
        string? nationality)
    {
        DisplayName = displayName;
        Nationality = nationality;
        Bio = bio;
        AvatarUrl = avatarUrl;
        if (preferredPosition.HasValue)
        {
            PreferredPosition = preferredPosition.Value;
            CanPlayGoalkeeper = preferredPosition == Position.Goalkeeper
                || (canPlayGoalkeeper ?? CanPlayGoalkeeper);
        }
        SkillLevel = skillLevel;
        UpdatedAt = DateTime.UtcNow;
    }
}
