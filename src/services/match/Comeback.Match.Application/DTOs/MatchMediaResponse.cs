namespace Comeback.Match.Application.DTOs;

public sealed record MatchMediaResponse(
    Guid Id,
    Guid UploadedByUserId,
    string UploaderDisplayName,
    string MediaType,
    string Url,
    string? ThumbnailUrl,
    string? Format,
    long? SizeInBytes,
    double? DurationInSeconds,
    int? Width,
    int? Height,
    DateTime CreatedAt);
