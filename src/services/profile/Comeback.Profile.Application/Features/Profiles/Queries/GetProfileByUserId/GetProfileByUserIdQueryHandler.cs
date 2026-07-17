namespace Comeback.Profile.Application.Features.Profiles.Queries.GetProfileByUserId;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.DTOs;
using MediatR;

internal sealed class GetProfileByUserIdQueryHandler : IRequestHandler<GetProfileByUserIdQuery, ProfileResponse>
{
    private readonly IUserProfileRepository _repository;

    public GetProfileByUserIdQueryHandler(IUserProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProfileResponse> Handle(GetProfileByUserIdQuery request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Profile not found.");

        return new ProfileResponse(
            profile.Id,
            profile.UserId,
            profile.Username,
            profile.Email,
            profile.FirstName,
            profile.LastName,
            profile.DateOfBirth,
            profile.PreferredPosition.ToString(),
            profile.CanPlayGoalkeeper,
            profile.YouthSeasons,
            profile.SeniorSeasons,
            profile.DisplayName,
            profile.Bio,
            profile.AvatarUrl,
            profile.SkillLevel?.ToString(),
            profile.CreatedAt,
            profile.UpdatedAt,
            profile.Nationality);
    }
}
