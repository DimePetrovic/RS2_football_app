namespace Comeback.Profile.Application.Features.Profiles.Commands.UpdateProfile;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.DTOs;
using Comeback.Profile.Domain.Enums;
using MediatR;

internal sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, ProfileResponse>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProfileCommandHandler(IUserProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProfileResponse> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException("Profile not found.");

        var position = command.Position is not null
            ? Enum.Parse<Position>(command.Position, ignoreCase: true)
            : (Position?)null;

        var skillLevel = command.SkillLevel is not null
            ? Enum.Parse<SkillLevel>(command.SkillLevel, ignoreCase: true)
            : (SkillLevel?)null;

        var nationality = NormalizeNationality(command.Nationality);
        profile.Update(command.DisplayName, command.Bio, command.AvatarUrl, position, command.CanPlayGoalkeeper, skillLevel, nationality);

        _repository.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(profile);
    }

    private static ProfileResponse ToResponse(Domain.Entities.UserProfile p) => new(
        p.Id,
        p.UserId,
        p.Username,
        p.Email,
        p.FirstName,
        p.LastName,
        p.DateOfBirth,
        p.PreferredPosition.ToString(),
        p.CanPlayGoalkeeper,
        p.YouthSeasons,
        p.SeniorSeasons,
        p.DisplayName,
        p.Bio,
        p.AvatarUrl,
        p.SkillLevel?.ToString(),
        p.CreatedAt,
        p.UpdatedAt,
        p.Nationality);

    /// <summary>ISO 3166-1 alpha-2 or null. "XK" is not an officially assigned ISO code and is rejected.</summary>
    private static string? NormalizeNationality(string? nationality)
    {
        if (string.IsNullOrWhiteSpace(nationality)) return null;
        var code = nationality.Trim().ToUpperInvariant();
        if (code.Length != 2 || !code.All(char.IsAsciiLetterUpper) || code == "XK")
            throw new Comeback.BuildingBlocks.Domain.Exceptions.BusinessRuleException(
                "Invalid nationality code.", "profile.invalid_nationality");
        return code;
    }
}
