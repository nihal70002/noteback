using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Note.Backend.Models;

namespace Note.Backend.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary? _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IOptions<CloudinaryOptions> options, ILogger<CloudinaryService> logger)
    {
        _logger = logger;
        var cloudinaryOptions = options.Value;

        // Debug: Log all environment variables
        _logger.LogInformation("=== CloudinaryService Environment Check ===");
        _logger.LogInformation("Options CloudName: {CloudName}", cloudinaryOptions.CloudName ?? "NULL");

        // Check raw environment variables
        var envCloudName1 = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
        var envCloudName2 = Environment.GetEnvironmentVariable("Cloudinary__CloudName");
        var envApiKey1 = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
        var envApiKey2 = Environment.GetEnvironmentVariable("Cloudinary__ApiKey");
        var envApiSecret1 = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");
        var envApiSecret2 = Environment.GetEnvironmentVariable("Cloudinary__ApiSecret");

        _logger.LogInformation("ENV CLOUDINARY_CLOUD_NAME: {Val}", envCloudName1 ?? "NOT SET");
        _logger.LogInformation("ENV Cloudinary__CloudName: {Val}", envCloudName2 ?? "NOT SET");
        _logger.LogInformation("ENV CLOUDINARY_API_KEY: {HasVal}", !string.IsNullOrEmpty(envApiKey1));
        _logger.LogInformation("ENV Cloudinary__ApiKey: {HasVal}", !string.IsNullOrEmpty(envApiKey2));
        _logger.LogInformation("ENV CLOUDINARY_API_SECRET: {HasVal}", !string.IsNullOrEmpty(envApiSecret1));
        _logger.LogInformation("ENV Cloudinary__ApiSecret: {HasVal}", !string.IsNullOrEmpty(envApiSecret2));

        // Fallback to environment variables if Options binding is empty
        var cloudName = cloudinaryOptions.CloudName
            ?? envCloudName1
            ?? envCloudName2;

        var apiKey = cloudinaryOptions.ApiKey
            ?? envApiKey1
            ?? envApiKey2;

        var apiSecret = cloudinaryOptions.ApiSecret
            ?? envApiSecret1
            ?? envApiSecret2;

        _logger.LogInformation("Final values - CloudName: {HasVal}, ApiKey: {HasVal}, ApiSecret: {HasVal}",
            !string.IsNullOrEmpty(cloudName),
            !string.IsNullOrEmpty(apiKey),
            !string.IsNullOrEmpty(apiSecret));

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            _logger.LogError("Cloudinary configuration is missing! Check Railway environment variables.");
            return;
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _logger.LogInformation("Cloudinary initialized successfully with cloud: {Cloud}", cloudName);
    }

    public async Task<(bool Success, string Url, string? ErrorMessage)> UploadAsync(IFormFile file, string folder)
    {
        _logger.LogInformation("=== UploadAsync called ===");

        if (_cloudinary is null)
        {
            _logger.LogError("Cloudinary client is null - configuration missing");
            return (false, string.Empty, "Cloudinary is not configured in backend settings.");
        }

        if (file.Length == 0)
        {
            _logger.LogWarning("File has zero length");
            return (false, string.Empty, "Empty file.");
        }

        var isVideo = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
        _logger.LogInformation("Uploading file: {FileName}, Type: {ContentType}, IsVideo: {IsVideo}, Folder: {Folder}",
            file.FileName, file.ContentType, isVideo, folder);

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

                _logger.LogInformation("Starting video upload to Cloudinary...");
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error is not null)
                {
                    _logger.LogError("Cloudinary video upload error: {Error}", uploadResult.Error.Message);
                    return (false, string.Empty, $"Cloudinary error: {uploadResult.Error.Message}");
                }

                _logger.LogInformation("Video upload successful: {Url}", uploadResult.SecureUrl);
                return (true, uploadResult.SecureUrl?.ToString() ?? string.Empty, null);
            }
            else
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder
                };

                _logger.LogInformation("Starting image upload to Cloudinary...");
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error is not null)
                {
                    _logger.LogError("Cloudinary image upload error: {Error}", uploadResult.Error.Message);
                    return (false, string.Empty, $"Cloudinary error: {uploadResult.Error.Message}");
                }

                _logger.LogInformation("Image upload successful: {Url}", uploadResult.SecureUrl);
                return (true, uploadResult.SecureUrl?.ToString() ?? string.Empty, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Cloudinary upload: {Message}", ex.Message);
            return (false, string.Empty, $"Upload exception: {ex.Message}");
        }
    }
}
