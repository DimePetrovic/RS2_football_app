namespace Comeback.Match.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;
using Comeback.Match.Domain.Enums;

public sealed class MatchMedia : Entity<Guid>
{
    public Guid MatchId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public string UploaderDisplayName { get; private set; } = string.Empty;
    public MatchMediaType MediaType { get; private set; }
    public string StorageProvider { get; private set; } = "Cloudinary";
    public string StoragePublicId { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string? ThumbnailUrl { get; private set; }
    public string? Format { get; private set; }
    public long? SizeInBytes { get; private set; }
    public double? DurationInSeconds { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public MatchMediaStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private MatchMedia() { }

    private MatchMedia(
        Guid id, Guid matchId, Guid uploadedByUserId, string uploaderDisplayName,
        MatchMediaType mediaType, string storagePublicId, string url, string? thumbnailUrl,
        string? format, long? sizeInBytes, double? durationInSeconds,
        int? width, int? height) : base(id)
    {
        MatchId = matchId;
        UploadedByUserId = uploadedByUserId;
        UploaderDisplayName = uploaderDisplayName;
        MediaType = mediaType;
        StoragePublicId = storagePublicId;
        Url = url;
        ThumbnailUrl = thumbnailUrl;
        Format = format;
        SizeInBytes = sizeInBytes;
        DurationInSeconds = durationInSeconds;
        Width = width;
        Height = height;
        Status = MatchMediaStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    public static MatchMedia Create(
        Guid matchId,
        Guid uploadedByUserId,
        string uploaderDisplayName,
        MatchMediaType mediaType,
        string storagePublicId,
        string url,
        string? thumbnailUrl,
        string? format,
        long? sizeInBytes,
        double? durationInSeconds,
        int? width,
        int? height)
        => new(Guid.NewGuid(), matchId, uploadedByUserId, uploaderDisplayName,
               mediaType, storagePublicId, url, thumbnailUrl,
               format, sizeInBytes, durationInSeconds, width, height);

    public void Remove() => Status = MatchMediaStatus.Removed;
}
