using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;

namespace Note.Backend.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary? _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(ILogger<CloudinaryService> logger)
    {
        _logger = logger;

        // Read Railway environment variables directly
        var cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
        var apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
        var apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");

        _logger.LogInformation("=== Cloudinary ENV check ===");
        _logger.LogInformation("CloudName exists: {Cloud}", !string.IsNullOrEmpty(cloudName));
        _logger.LogInformation("ApiKey exists: {Key}", !string.IsNullOrEmpty(apiKey));
        _logger.LogInformation("ApiSecret exists: {Secret}", !string.IsNullOrEmpty(apiSecret));

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            _logger.LogError("Cloudinary environment variables missing.");
            return;
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);

        _logger.LogInformation("Cloudinary initialized successfully.");
    }

    public async Task<(bool Success, string Url, string? ErrorMessage)> UploadAsync(IFormFile file, string folder)
    {
        if (_cloudinary is null)
        {
            _logger.LogError("Cloudinary client is null.");
            return (false, string.Empty, "Cloudinary is not configured.");
        }

        if (file.Length == 0)
        {
            _logger.LogWarning("Empty file received.");
            return (false, string.Empty, "File is empty.");
        }

        var isVideo = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

        try
        {
            await using var stream = file.OpenReadStream();

            if (isVideo)
            {
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.Error != null)
                {
                    _logger.LogError("Cloudinary video upload error: {Error}", result.Error.Message);
                    return (false, string.Empty, result.Error.Message);
                }

                return (true, result.SecureUrl.ToString(), null);
            }
            else
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.Error != null)
                {
                    _logger.LogError("Cloudinary image upload error: {Error}", result.Error.Message);
                    return (false, string.Empty, result.Error.Message);
                }

                return (true, result.SecureUrl.ToString(), null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload exception");
            return (false, string.Empty, ex.Message);
        }
    }
}