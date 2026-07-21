namespace Comeback.Rating.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Profile;
using Comeback.Rating.Application.Features.Xp.Commands.UpdateCareerXp;
using MassTransit;
using MediatR;

internal sealed class PlayerCareerDataUpdatedConsumer : IConsumer<PlayerCareerDataUpdatedIntegrationEvent>
{
    private readonly ISender _sender;

    public PlayerCareerDataUpdatedConsumer(ISender sender)
    {
        _sender = sender;
    }

    public async Task Consume(ConsumeContext<PlayerCareerDataUpdatedIntegrationEvent> context)
    {
        var message = context.Message;
        await _sender.Send(
            new UpdateCareerXpCommand(
                message.UserId,
                message.YouthSeasons,
                message.SeniorSeasons),
            context.CancellationToken);
    }
}
