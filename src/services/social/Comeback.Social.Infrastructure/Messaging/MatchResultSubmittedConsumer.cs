namespace Comeback.Social.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Social.Application.Features.Posts.Commands.CreateMatchResultPost;
using MassTransit;
using MediatR;

public sealed class MatchResultSubmittedConsumer : IConsumer<MatchResultSubmittedIntegrationEvent>
{
    private readonly ISender _sender;

    public MatchResultSubmittedConsumer(ISender sender) => _sender = sender;

    public async Task Consume(ConsumeContext<MatchResultSubmittedIntegrationEvent> context)
    {
        var e = context.Message;

        await _sender.Send(new CreateMatchResultPostCommand(
            e.MatchId,
            e.MatchTitle,
            e.HomeScore,
            e.AwayScore,
            e.Participants.Select(p => new ParticipantDto(p.UserId, p.DisplayName)).ToList()),
            context.CancellationToken);
    }
}
