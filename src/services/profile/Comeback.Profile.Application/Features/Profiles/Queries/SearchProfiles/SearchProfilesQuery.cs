namespace Comeback.Profile.Application.Features.Profiles.Queries.SearchProfiles;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.Profile.Application.DTOs;

public sealed record SearchProfilesQuery(string Query, Guid ExcludeUserId) : IQuery<List<ProfileSearchResult>>;
