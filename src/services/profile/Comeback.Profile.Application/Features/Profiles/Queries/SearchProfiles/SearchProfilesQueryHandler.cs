namespace Comeback.Profile.Application.Features.Profiles.Queries.SearchProfiles;

using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.DTOs;
using MediatR;

internal sealed class SearchProfilesQueryHandler : IRequestHandler<SearchProfilesQuery, List<ProfileSearchResult>>
{
    private readonly IUserProfileRepository _profiles;

    public SearchProfilesQueryHandler(IUserProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public async Task<List<ProfileSearchResult>> Handle(SearchProfilesQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Query) || query.Query.Length < 2)
            return [];

        var profiles = await _profiles.SearchAsync(query.Query.Trim(), query.ExcludeUserId, limit: 10, cancellationToken);

        return profiles.Select(p => new ProfileSearchResult(
            p.UserId,
            p.Username,
            p.FirstName,
            p.LastName,
            p.DisplayName,
            p.AvatarUrl,
            p.Nationality)).ToList();
    }
}
