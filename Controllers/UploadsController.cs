using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Note.Backend.Services;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UploadsController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(ICloudinaryService cloudinaryService, IConfiguration configuration, ILogger<UploadsController> logger)
    {
        _cloudinaryService = cloudinaryService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("cloudinary")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(1024L * 1024L * 100L)] // 100 MB
    public async Task<IActionResult> UploadToCloudinary()
    {
        // Debug logging
        _logger.LogInformation("=== Cloudinary Upload Debug ===");
        _logger.LogInformation("Content-Type: {ContentType}", Request.ContentType);
        _logger.LogInformation("Form file count: {Count}", Request.Form.Files.Count);
        _logger.LogInformation("Form keys: {Keys}", string.Join(", ", Request.Form.Keys));

        // Check Cloudinary config
        var cloudName = _configuration["Cloudinary:CloudName"] ?? Environment.GetEnvironmentVariable("Cloudinary__CloudName");
        var apiKey = _configuration["Cloudinary:ApiKey"] ?? Environment.GetEnvironmentVariable("Cloudinary__ApiKey");
        var apiSecret = _configuration["Cloudinary:ApiSecret"] ?? Environment.GetEnvironmentVariable("Cloudinary__ApiSecret");

        _logger.LogInformation("Cloudinary Config - CloudName: {HasCloudName}, ApiKey: {HasApiKey}, ApiSecret: {HasApiSecret}",
            !string.IsNullOrEmpty(cloudName),
            !string.IsNullOrEmpty(apiKey),
            !string.IsNullOrEmpty(apiSecret));

        var file = Request.Form.Files.FirstOrDefault();
        if (file is null)
        {
            _logger.LogWarning("No file found in request");
            return BadRequest(new { Message = "Please select a file.", Debug = "No file in Request.Form.Files" });
        }

        _logger.LogInformation("File received: {FileName}, Size: {Size}, ContentType: {ContentType}",
            file.FileName, file.Length, file.ContentType);

        var folder = Request.Form["folder"].FirstOrDefault();
        var targetFolder = string.IsNullOrWhiteSpace(folder) ? "note/products" : folder.Trim();

        _logger.LogInformation("Target folder: {Folder}", targetFolder);

        var uploadResult = await _cloudinaryService.UploadAsync(file, targetFolder);

        if (!uploadResult.Success)
        {
            _logger.LogError("Upload failed: {Error}", uploadResult.ErrorMessage);
            if (uploadResult.ErrorMessage?.Contains("not configured", StringComparison.OrdinalIgnoreCase) == true)
            {
                return BadRequest(new
                {
                    Message = "Cloudinary is not configured. Please set environment variables.",
                    Debug = "Cloudinary config missing",
                    RequiredVars = new[] { "CLOUDINARY_CLOUD_NAME", "CLOUDINARY_API_KEY", "CLOUDINARY_API_SECRET" }
                });
            }
            return BadRequest(new { Message = uploadResult.ErrorMessage ?? "Upload failed.", Debug = "Cloudinary upload failed" });
        }

        _logger.LogInformation("Upload successful: {Url}", uploadResult.Url);
        return Ok(new { Url = uploadResult.Url });
    }
}
