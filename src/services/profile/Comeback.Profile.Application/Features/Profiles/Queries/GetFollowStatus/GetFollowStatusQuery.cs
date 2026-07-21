namespace Comeback.Profile.Application.Features.Profiles.Queries.GetFollowStatus;

using MediatR;

public sealed record GetFollowStatusQuery(Guid CurrentUserId, Guid TargetUserId) : IRequest<bool>;
