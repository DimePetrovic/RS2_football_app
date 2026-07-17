namespace Comeback.Profile.Application.Features.Groups.Commands.DeleteGroup;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record DeleteGroupCommand(
    Guid GroupId,
    Guid RequestingUserId) : ICommand;
