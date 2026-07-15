namespace Comeback.Auth.Application.Features.Auth.Commands.CompleteRegistration;

using Comeback.Auth.Application.DTOs;
using Comeback.BuildingBlocks.Application.Messaging;

public sealed record CompleteRegistrationCommand(
    string UserId,
    string Token,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    int PreferredPosition,
    bool CanPlayGoalkeeper,
    int YouthSeasons,
    int SeniorSeasons,
    string? Nationality,
    string IpAddress) : ICommand<AuthResponse>;
