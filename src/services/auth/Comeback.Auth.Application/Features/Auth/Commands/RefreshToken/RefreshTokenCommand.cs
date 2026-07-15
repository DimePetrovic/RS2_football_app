namespace Comeback.Auth.Application.Features.Auth.Commands.RefreshToken;

using Comeback.Auth.Application.DTOs;
using Comeback.BuildingBlocks.Application.Messaging;

public sealed record RefreshTokenCommand(
    string Token,
    string IpAddress) : ICommand<AuthResponse>;
