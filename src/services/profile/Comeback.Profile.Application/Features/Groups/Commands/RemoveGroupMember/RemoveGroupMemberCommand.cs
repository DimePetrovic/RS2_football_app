namespace Comeback.Profile.Application.Features.Groups.Commands.RemoveGroupMember;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record RemoveGroupMemberCommand(
    Guid GroupId,
    Guid RequestingUserId,
    Guid MemberUserId) : ICommand;
