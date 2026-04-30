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
        if (string.IsNullOrWhiteSpace(cloudinaryOptions.CloudName) ||
            string.IsNullOrWhiteSpace(cloudinaryOptions.ApiKey) ||
            string.IsNullOrWhiteSpace(cloudinaryOptions.ApiSecret))
        {
            return;
        }

        var account = new Account(
            cloudinaryOptions.CloudName,
            cloudinaryOptions.ApiKey,
            cloudinaryOptions.ApiSecret
        );

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
