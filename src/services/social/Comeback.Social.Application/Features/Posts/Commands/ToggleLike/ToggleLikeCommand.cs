namespace Comeback.Social.Application.Features.Posts.Commands.ToggleLike;

using MediatR;

public sealed record ToggleLikeCommand(Guid PostId, Guid UserId) : IRequest<bool>;
