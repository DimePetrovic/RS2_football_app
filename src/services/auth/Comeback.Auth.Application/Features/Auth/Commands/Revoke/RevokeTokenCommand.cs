namespace Comeback.Auth.Application.Features.Auth.Commands.Revoke;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record RevokeTokenCommand(
    string Token,
    string IpAddress) : ICommand;
