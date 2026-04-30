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

    public UploadsController(ICloudinaryService cloudinaryService)
    {
        _cloudinaryService = cloudinaryService;
    }

    [HttpPost("cloudinary")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(1024L * 1024L * 100L)] // 100 MB
    public async Task<IActionResult> UploadToCloudinary()
    {
        var file = Request.Form.Files.FirstOrDefault();
        if (file is null)
        {
            return BadRequest(new { Message = "Please select a file." });
        }

        var folder = Request.Form["folder"].FirstOrDefault();
        var targetFolder = string.IsNullOrWhiteSpace(folder) ? "note/products" : folder.Trim();
        var uploadResult = await _cloudinaryService.UploadAsync(file, targetFolder);

        if (!uploadResult.Success)
        {
            return BadRequest(new { Message = uploadResult.ErrorMessage ?? "Upload failed." });
        }

        return Ok(new { Url = uploadResult.Url });
    }
}
