namespace Comeback.Social.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Social.Application.Features.Posts.Commands.CreatePlayerWantedPost;
using MassTransit;
using MediatR;

public sealed class PlayerWantedConsumer : IConsumer<PlayerWantedIntegrationEvent>
{
    private readonly ISender _sender;

    public PlayerWantedConsumer(ISender sender) => _sender = sender;

    public async Task Consume(ConsumeContext<PlayerWantedIntegrationEvent> context)
    {
        var e = context.Message;
        await _sender.Send(new CreatePlayerWantedPostCommand(
            e.MatchId, e.MatchTitle, e.OrganizerUserId, e.OrganizerDisplayName,
            e.Position, e.Location, e.StartsAt), context.CancellationToken);
    }
}
