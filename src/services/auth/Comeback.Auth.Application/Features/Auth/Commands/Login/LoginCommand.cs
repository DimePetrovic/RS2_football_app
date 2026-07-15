namespace Comeback.Auth.Application.Features.Auth.Commands.Login;

using Comeback.Auth.Application.DTOs;
using Comeback.BuildingBlocks.Application.Messaging;

public sealed record LoginCommand(
    string Email,
    string Password,
    string IpAddress) : ICommand<AuthResponse>;
