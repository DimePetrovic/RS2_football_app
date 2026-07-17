namespace Comeback.BuildingBlocks.Infrastructure.Media;

/// <summary>
/// Parameters of a signed upload — the frontend sends them directly to the Cloudinary API,
/// together with the file, exactly in this shape (the signature covers folder + timestamp).
/// </summary>
public sealed record CloudinaryUploadSignature(
    string CloudName,
    string ApiKey,
    long Timestamp,
    string Folder,
    string Signature);
