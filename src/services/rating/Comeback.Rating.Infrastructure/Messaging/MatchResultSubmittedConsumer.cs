namespace Comeback.Rating.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Rating.Application.Features.Xp.Commands.AwardMatchXp;
using MassTransit;
using MediatR;

internal sealed class MatchResultSubmittedConsumer : IConsumer<MatchResultSubmittedIntegrationEvent>
{
    private readonly ISender _sender;

    public MatchResultSubmittedConsumer(ISender sender) => _sender = sender;

    public async Task Consume(ConsumeContext<MatchResultSubmittedIntegrationEvent> context)
    {
        var e = context.Message;
        if (e.Players.Count == 0) return;

        bool isDraw = e.HomeScore == e.AwayScore;
        string winnerTeam = e.HomeScore > e.AwayScore ? "Home" : "Away";

        foreach (var player in e.Players)
        {
            int xp = MatchXpRules.Calculate(
                isWinner: !isDraw && player.Team == winnerTeam,
                isDraw: isDraw,
                isCaptain: player.IsCaptain);

            await _sender.Send(
                new AwardMatchXpCommand(player.UserId, xp),
                context.CancellationToken);
        }
    }
}
