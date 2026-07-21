namespace Comeback.Social.Application.Features.Posts.Commands.CreateMatchResultPost;

using MediatR;

public sealed record ParticipantDto(Guid UserId, string DisplayName);

public sealed record CreateMatchResultPostCommand(
    Guid MatchId,
    string MatchTitle,
    int HomeScore,
    int AwayScore,
    IReadOnlyList<ParticipantDto> Participants) : IRequest;
