namespace Comeback.Profile.Application.Features.Profiles.Commands.UpdateProfile;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.Profile.Application.DTOs;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? Position,
    bool? CanPlayGoalkeeper,
    string? SkillLevel,
    string? Nationality) : ICommand<ProfileResponse>;
