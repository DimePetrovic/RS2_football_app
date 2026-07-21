namespace Comeback.Social.Application.Features.Posts.Commands.AddComment;

using Comeback.Social.Application.DTOs;
using MediatR;

public sealed record AddCommentCommand(
    Guid PostId, Guid AuthorUserId, string AuthorDisplayName, string Content) : IRequest<CommentResponse>;
