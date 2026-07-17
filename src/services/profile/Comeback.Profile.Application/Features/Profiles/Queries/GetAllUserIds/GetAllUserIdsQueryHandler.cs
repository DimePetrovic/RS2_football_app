namespace Comeback.Profile.Application.Features.Profiles.Queries.GetAllUserIds;
using Comeback.Profile.Application.Common.Interfaces;
using MediatR;

internal sealed class GetAllUserIdsQueryHandler : IRequestHandler<GetAllUserIdsQuery, List<Guid>>
{
    private readonly IUserProfileRepository _profiles;

    public GetAllUserIdsQueryHandler(IUserProfileRepository profiles) => _profiles = profiles;

    public async Task<List<Guid>> Handle(GetAllUserIdsQuery query, CancellationToken ct)
    {
        var profiles = await _profiles.GetAllAsync(ct);
        return profiles.Select(p => p.UserId).ToList();
    }
}
