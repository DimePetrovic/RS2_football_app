namespace Comeback.Social.Application.Features.Posts.Commands.CreatePlayerWantedPost;
using MediatR;

public sealed record CreatePlayerWantedPostCommand(
    Guid MatchId,
    string MatchTitle,
    Guid OrganizerUserId,
    string OrganizerDisplayName,
    string? Position,
    string? Location,
    DateTime StartsAt) : IRequest;
