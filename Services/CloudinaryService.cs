using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using Note.Backend.Models;

namespace Note.Backend.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary? _cloudinary;

    public CloudinaryService(IOptions<CloudinaryOptions> options)
    {
        var cloudinaryOptions = options.Value;

        // Fallback to environment variables if Options binding is empty
        var cloudName = cloudinaryOptions.CloudName
            ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
            ?? Environment.GetEnvironmentVariable("Cloudinary__CloudName");

        var apiKey = cloudinaryOptions.ApiKey
            ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")
            ?? Environment.GetEnvironmentVariable("Cloudinary__ApiKey");

        var apiSecret = cloudinaryOptions.ApiSecret
            ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
            ?? Environment.GetEnvironmentVariable("Cloudinary__ApiSecret");

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            return;
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<(bool Success, string Url, string? ErrorMessage)> UploadAsync(IFormFile file, string folder)
    {
        if (_cloudinary is null)
        {
            return (false, string.Empty, "Cloudinary is not configured in backend settings.");
        }

        if (file.Length == 0)
        {
            return (false, string.Empty, "Empty file.");
        }

        var isVideo = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

        await using var stream = file.OpenReadStream();
        if (isVideo)
        {
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.Error is null
                ? (true, uploadResult.SecureUrl?.ToString() ?? string.Empty, null)
                : (false, string.Empty, uploadResult.Error.Message);
        }
        else
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.Error is null
                ? (true, uploadResult.SecureUrl?.ToString() ?? string.Empty, null)
                : (false, string.Empty, uploadResult.Error.Message);
        }
    }
}
