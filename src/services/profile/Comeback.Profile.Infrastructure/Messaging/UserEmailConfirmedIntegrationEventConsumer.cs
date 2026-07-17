namespace Comeback.Profile.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Auth;
using Comeback.Profile.Application.Features.Profiles.Commands.CreateProfile;
using MassTransit;
using MediatR;

internal sealed class UserEmailConfirmedIntegrationEventConsumer : IConsumer<UserEmailConfirmedIntegrationEvent>
{
    private readonly ISender _sender;

    public UserEmailConfirmedIntegrationEventConsumer(ISender sender)
    {
        _sender = sender;
    }

    public async Task Consume(ConsumeContext<UserEmailConfirmedIntegrationEvent> context)
    {
        var message = context.Message;
        await _sender.Send(
            new CreateProfileCommand(
                message.UserId,
                message.Username,
                message.Email,
                message.FirstName,
                message.LastName,
                message.DateOfBirth,
                message.PreferredPosition,
                message.CanPlayGoalkeeper,
                message.YouthSeasons,
                message.SeniorSeasons,
                message.Role,
                message.Nationality),
            context.CancellationToken);
    }
}
