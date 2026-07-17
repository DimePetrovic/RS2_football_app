namespace Comeback.BuildingBlocks.Infrastructure.Media;

public interface ICloudinaryMediaService
{
    /// <summary>Creates a signature for a direct browser upload into the given folder.</summary>
    CloudinaryUploadSignature CreateUploadSignature(string folder);

    /// <summary>Deletes a file from Cloudinary storage. <paramref name="resourceType"/> is "image" or "video".</summary>
    Task DeleteAsync(string publicId, string resourceType, CancellationToken ct = default);
}
