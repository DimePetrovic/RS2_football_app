namespace Comeback.Profile.Application.Features.Profiles.Queries.GetFollowers;
using Comeback.Profile.Application.DTOs;
using MediatR;

public sealed record GetFollowersQuery(Guid UserId) : IRequest<List<ProfileSearchResult>>;
