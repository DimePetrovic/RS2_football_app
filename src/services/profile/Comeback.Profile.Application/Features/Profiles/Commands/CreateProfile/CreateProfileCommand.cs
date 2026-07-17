namespace Comeback.Profile.Application.Features.Profiles.Commands.CreateProfile;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record CreateProfileCommand(
    Guid UserId,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    int PreferredPosition,
    bool CanPlayGoalkeeper,
    int YouthSeasons,
    int SeniorSeasons,
    string Role,
    string? Nationality) : ICommand;
