namespace Comeback.Profile.Application.Features.Groups.Commands.CreateGroup;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.Profile.Application.DTOs;

public sealed record CreateGroupCommand(
    Guid RequestingUserId,
    string Name,
    string? AvatarUrl,
    List<Guid> MemberUserIds) : ICommand<GroupSummaryResponse>;
