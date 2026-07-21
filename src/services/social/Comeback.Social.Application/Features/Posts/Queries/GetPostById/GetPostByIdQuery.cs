namespace Comeback.Social.Application.Features.Posts.Queries.GetPostById;

using Comeback.Social.Application.DTOs;
using MediatR;

public sealed record GetPostByIdQuery(Guid PostId, Guid CurrentUserId) : IRequest<PostResponse>;
