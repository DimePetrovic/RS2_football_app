namespace Comeback.Profile.Application.Features.Profiles.Queries.GetFollowCounts;
using MediatR;

public sealed record FollowCountsResponse(int Followers, int Following);

public sealed record GetFollowCountsQuery(Guid UserId) : IRequest<FollowCountsResponse>;
