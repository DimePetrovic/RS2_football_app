namespace Comeback.Auth.Application.Features.Auth.Commands.ResendConfirmationEmail;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record ResendConfirmationEmailCommand(string Email) : ICommand;
