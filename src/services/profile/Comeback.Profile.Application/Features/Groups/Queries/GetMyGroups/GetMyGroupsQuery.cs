namespace Comeback.Profile.Application.Features.Groups.Queries.GetMyGroups;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.Profile.Application.DTOs;

public sealed record GetMyGroupsQuery(Guid UserId) : IQuery<List<GroupSummaryResponse>>;
