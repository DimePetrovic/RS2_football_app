namespace Comeback.Profile.Application.Features.Profiles.Queries.GetFollowStatus;

using Comeback.Profile.Application.Common.Interfaces;
using MediatR;

internal sealed class GetFollowStatusQueryHandler : IRequestHandler<GetFollowStatusQuery, bool>
{
    private readonly IPlayerFollowRepository _follows;

    public GetFollowStatusQueryHandler(IPlayerFollowRepository follows) => _follows = follows;

    public async Task<bool> Handle(GetFollowStatusQuery query, CancellationToken cancellationToken)
    {
        var follow = await _follows.GetAsync(query.CurrentUserId, query.TargetUserId, cancellationToken);
        return follow is not null;
    }
}
