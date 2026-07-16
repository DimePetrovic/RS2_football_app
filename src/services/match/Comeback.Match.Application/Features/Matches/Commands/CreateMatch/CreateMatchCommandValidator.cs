namespace Comeback.Match.Application.Features.Matches.Commands.CreateMatch;

using FluentValidation;

public sealed class CreateMatchCommandValidator : AbstractValidator<CreateMatchCommand>
{
    public CreateMatchCommandValidator()
    {
        // The title is optional on the form — the frontend sends a default title when empty.
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(500)
            .WithMessage("Match location is required.").WithErrorCode("match.location_required");
    }
}
