namespace Comeback.Profile.Application.Features.Profiles.Queries.GetFollowing;

using Comeback.Profile.Application.DTOs;
using MediatR;

public sealed record GetFollowingQuery(Guid UserId) : IRequest<List<ProfileSearchResult>>;
