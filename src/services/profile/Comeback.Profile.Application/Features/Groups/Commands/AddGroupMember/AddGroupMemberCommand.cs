namespace Comeback.Profile.Application.Features.Groups.Commands.AddGroupMember;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record AddGroupMemberCommand(
    Guid GroupId,
    Guid RequestingUserId,
    Guid MemberUserId) : ICommand;
