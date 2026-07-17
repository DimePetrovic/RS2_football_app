namespace Comeback.Profile.Application.Features.Profiles.Commands.CreateProfile;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.BuildingBlocks.IntegrationEvents.Profile;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Domain.Entities;
using MediatR;

internal sealed class CreateProfileCommandHandler : IRequestHandler<CreateProfileCommand>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _publisher;

    public CreateProfileCommandHandler(
        IUserProfileRepository repository,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher publisher)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task Handle(CreateProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = UserProfile.Create(
            command.UserId,
            command.Username,
            command.Email,
            command.FirstName,
            command.LastName,
            command.DateOfBirth,
            (Comeback.Profile.Domain.Enums.Position)command.PreferredPosition,
            command.CanPlayGoalkeeper,
            command.YouthSeasons,
            command.SeniorSeasons,
            command.Role,
            command.Nationality);

        _repository.Add(profile);

        await _publisher.PublishAsync(
            new PlayerCareerDataUpdatedIntegrationEvent(
                command.UserId,
                command.YouthSeasons,
                command.SeniorSeasons),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
