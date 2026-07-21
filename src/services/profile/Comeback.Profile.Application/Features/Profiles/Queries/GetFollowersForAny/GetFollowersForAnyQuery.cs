namespace Comeback.Profile.Application.Features.Profiles.Queries.GetFollowersForAny;

using MediatR;

public sealed record GetFollowersForAnyQuery(IReadOnlyList<Guid> UserIds) : IRequest<List<Guid>>;
