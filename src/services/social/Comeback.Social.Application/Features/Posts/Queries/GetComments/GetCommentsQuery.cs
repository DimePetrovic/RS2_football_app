namespace Comeback.Social.Application.Features.Posts.Queries.GetComments;

using Comeback.Social.Application.DTOs;
using MediatR;

public sealed record GetCommentsQuery(Guid PostId) : IRequest<List<CommentResponse>>;
