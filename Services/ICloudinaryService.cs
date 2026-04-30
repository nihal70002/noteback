namespace Note.Backend.Services;

public interface ICloudinaryService
{
    Task<(bool Success, string Url, string? ErrorMessage)> UploadAsync(IFormFile file, string folder);
}
