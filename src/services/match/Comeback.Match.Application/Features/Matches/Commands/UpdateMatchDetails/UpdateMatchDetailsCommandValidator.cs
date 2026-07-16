namespace Comeback.Match.Application.Features.Matches.Commands.UpdateMatchDetails;

using FluentValidation;

public sealed class UpdateMatchDetailsCommandValidator : AbstractValidator<UpdateMatchDetailsCommand>
{
    public UpdateMatchDetailsCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(500)
            .WithMessage("Match location is required.").WithErrorCode("match.location_required");
    }
}
