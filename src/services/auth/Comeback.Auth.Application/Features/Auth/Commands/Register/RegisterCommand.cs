namespace Comeback.Auth.Application.Features.Auth.Commands.Register;

using Comeback.Auth.Application.DTOs;
using Comeback.BuildingBlocks.Application.Messaging;

public sealed record RegisterCommand(
    string Email,
    string Username,
    string Password,
    string ConfirmPassword,
    string IpAddress) : ICommand<RegistrationResponse>;
