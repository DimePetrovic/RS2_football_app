namespace Comeback.Profile.Application.Features.Profiles.Commands.UpdateProfile;

using Comeback.Profile.Domain.Enums;
using FluentValidation;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    private static readonly string[] ValidPositions = Enum.GetNames<Position>();
    private static readonly string[] ValidSkillLevels = Enum.GetNames<SkillLevel>();

    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(100)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.Bio)
            .MaximumLength(500)
            .When(x => x.Bio is not null);

        RuleFor(x => x.AvatarUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("AvatarUrl must be a valid URL.")
            .When(x => x.AvatarUrl is not null);

        RuleFor(x => x.Position)
            .Must(p => ValidPositions.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Position must be one of: {string.Join(", ", ValidPositions)}.")
            .When(x => x.Position is not null);

        RuleFor(x => x.SkillLevel)
            .Must(s => ValidSkillLevels.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SkillLevel must be one of: {string.Join(", ", ValidSkillLevels)}.")
            .When(x => x.SkillLevel is not null);
    }
}
