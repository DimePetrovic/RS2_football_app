namespace Comeback.BuildingBlocks.Infrastructure.Media;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class CloudinaryMediaService : ICloudinaryMediaService
{
    private readonly HttpClient _http;
    private readonly CloudinaryOptions _options;
    private readonly ILogger<CloudinaryMediaService> _logger;

    public CloudinaryMediaService(
        HttpClient http,
        IOptions<CloudinaryOptions> options,
        ILogger<CloudinaryMediaService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public CloudinaryUploadSignature CreateUploadSignature(string folder)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = Sign(new SortedDictionary<string, string>
        {
            ["folder"] = folder,
            ["timestamp"] = timestamp.ToString(),
        });

        return new CloudinaryUploadSignature(
            _options.CloudName, _options.ApiKey, timestamp, folder, signature);
    }

    public async Task DeleteAsync(string publicId, string resourceType, CancellationToken ct = default)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = Sign(new SortedDictionary<string, string>
        {
            ["public_id"] = publicId,
            ["timestamp"] = timestamp.ToString(),
        });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["public_id"] = publicId,
            ["timestamp"] = timestamp.ToString(),
            ["api_key"] = _options.ApiKey,
            ["signature"] = signature,
        });

        var response = await _http.PostAsync(
            $"https://api.cloudinary.com/v1_1/{_options.CloudName}/{resourceType}/destroy", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "Cloudinary destroy failed for {PublicId} ({StatusCode}): {Body}",
                publicId, (int)response.StatusCode, body);
        }
    }

    // Cloudinary potpis: SHA-1 heksadecimalno od "key1=value1&key2=value2" (sortirano) + api_secret.
    private string Sign(SortedDictionary<string, string> parameters)
    {
        var toSign = string.Join("&", parameters.Select(kv => $"{kv.Key}={kv.Value}")) + _options.ApiSecret;
        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(toSign))).ToLowerInvariant();
    }
}
