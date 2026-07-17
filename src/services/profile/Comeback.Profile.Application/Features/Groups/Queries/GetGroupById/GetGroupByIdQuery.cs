namespace Comeback.Profile.Application.Features.Groups.Queries.GetGroupById;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.Profile.Application.DTOs;

public sealed record GetGroupByIdQuery(Guid GroupId, Guid RequestingUserId) : IQuery<GroupDetailResponse>;
