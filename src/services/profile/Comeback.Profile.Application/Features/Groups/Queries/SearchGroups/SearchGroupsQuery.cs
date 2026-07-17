namespace Comeback.Profile.Application.Features.Groups.Queries.SearchGroups;

using Comeback.Profile.Application.DTOs;
using MediatR;

public sealed record SearchGroupsQuery(string Query, Guid? ExcludeOverlappingWithGroupId) : IRequest<List<GroupSearchResult>>;
