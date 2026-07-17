namespace Comeback.Profile.Application.Features.Groups.Commands.UpdateGroup;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record UpdateGroupCommand(
    Guid GroupId,
    Guid RequestingUserId,
    string Name,
    string? AvatarUrl) : ICommand;
