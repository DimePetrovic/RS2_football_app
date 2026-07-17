namespace Comeback.Profile.Application.Features.Groups.Queries.SearchGroups;

using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.DTOs;
using MediatR;

internal sealed class SearchGroupsQueryHandler : IRequestHandler<SearchGroupsQuery, List<GroupSearchResult>>
{
    private readonly IPlayerGroupRepository _groups;

    public SearchGroupsQueryHandler(IPlayerGroupRepository groups) => _groups = groups;

    public async Task<List<GroupSearchResult>> Handle(SearchGroupsQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Query) || query.Query.Length < 2)
            return [];

        var groups = await _groups.SearchByNameAsync(query.Query.Trim(), limit: 10, cancellationToken);

        if (query.ExcludeOverlappingWithGroupId.HasValue)
        {
            var ownGroup = await _groups.GetByIdWithMembersAsync(query.ExcludeOverlappingWithGroupId.Value, cancellationToken);
            if (ownGroup is not null)
            {
                var ownMemberIds = ownGroup.Members.Select(m => m.ProfileId).ToHashSet();
                groups = groups
                    .Where(g => !g.Members.Any(m => ownMemberIds.Contains(m.ProfileId)))
                    .ToList();
            }
        }

        return groups.Select(g => new GroupSearchResult(g.Id, g.Name, g.AvatarUrl, g.Members.Count)).ToList();
    }
}
