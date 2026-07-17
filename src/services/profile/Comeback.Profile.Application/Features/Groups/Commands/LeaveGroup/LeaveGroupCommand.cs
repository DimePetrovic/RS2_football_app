namespace Comeback.Profile.Application.Features.Groups.Commands.LeaveGroup;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record LeaveGroupCommand(
    Guid GroupId,
    Guid RequestingUserId) : ICommand;
