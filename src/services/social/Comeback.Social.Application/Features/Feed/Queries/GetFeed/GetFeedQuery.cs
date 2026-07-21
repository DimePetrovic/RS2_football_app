namespace Comeback.Social.Application.Features.Feed.Queries.GetFeed;

using Comeback.Social.Application.DTOs;
using MediatR;

public sealed record GetFeedQuery(Guid UserId, int Page, int PageSize) : IRequest<List<PostResponse>>;
