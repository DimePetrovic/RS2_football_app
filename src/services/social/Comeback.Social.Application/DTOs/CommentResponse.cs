namespace Comeback.Social.Application.DTOs;

public sealed record CommentResponse(
    Guid Id,
    Guid AuthorUserId,
    string AuthorDisplayName,
    string Content,
    DateTime CreatedAt);
