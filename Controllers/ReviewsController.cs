using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;
using Note.Backend.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/products/{productId}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly NoteDbContext _context;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(NoteDbContext context, ICloudinaryService cloudinaryService, ILogger<ReviewsController> logger)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviews(string productId)
    {
        var reviews = await _context.ProductReviews
            .Where(r => r.ProductId == productId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                Username = r.User != null ? r.User.Username : "Customer",
                r.Images
            })
            .ToListAsync();

        var response = reviews.Select(r => new
        {
            r.Id,
            r.Rating,
            r.Comment,
            r.CreatedAt,
            r.Username,
            Images = DeserializeReviewImages(r.Images)
        });

        _logger.LogInformation("Returning {ReviewCount} reviews for product {ProductId}", reviews.Count, productId);

        return Ok(response);
    }

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(1024L * 1024L * 15L)] // 15MB for multiple images
    public async Task<IActionResult> UpsertReview(string productId, [FromForm] IFormFileCollection? files, [FromForm] string? rating, [FromForm] string? comment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (!int.TryParse(rating, out var ratingValue) || ratingValue < 1 || ratingValue > 5)
        {
            return BadRequest(new { Message = "Rating must be between 1 and 5." });
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null) return NotFound(new { Message = "Product not found." });

        var review = await _context.ProductReviews.FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
        if (review == null)
        {
            review = new ProductReview { ProductId = productId, UserId = userId };
            _context.ProductReviews.Add(review);
        }

        review.Rating = ratingValue;
        review.Comment = comment?.Trim() ?? string.Empty;
        review.CreatedAt = DateTime.UtcNow;

        // Handle image uploads
        var uploadedFiles = Request.HasFormContentType
            ? Request.Form.Files
                .Where(file =>
                    file.Name.Equals("images", StringComparison.OrdinalIgnoreCase) ||
                    file.Name.Equals("files", StringComparison.OrdinalIgnoreCase))
                .ToList()
            : files?.ToList() ?? [];

        if (uploadedFiles.Count == 0 && files is { Count: > 0 })
        {
            uploadedFiles = files.ToList();
        }

        _logger.LogInformation("Review upload for product {ProductId}: received {FileCount} files", productId, uploadedFiles.Count);

        var imageUrls = new List<string>();
        if (uploadedFiles.Count > 0)
        {
            foreach (var file in uploadedFiles.Take(3)) // Max 3 images
            {
                _logger.LogInformation(
                    "Uploading review image {FileName} ({ContentType}, {Length} bytes)",
                    file.FileName,
                    file.ContentType,
                    file.Length);

                if (file.Length > 5 * 1024 * 1024) // 5MB per image
                {
                    return BadRequest(new { Message = $"Image {file.FileName} is too large. Maximum size is 5MB per image." });
                }

                if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { Message = $"File {file.FileName} is not a valid image." });
                }

                var uploadResult = await _cloudinaryService.UploadAsync(file, "reviews");
                if (!uploadResult.Success)
                {
                    return BadRequest(new { Message = $"Failed to upload image: {uploadResult.ErrorMessage}" });
                }

                imageUrls.Add(uploadResult.Url);
            }

            review.Images = JsonSerializer.Serialize(imageUrls);
        }

        await _context.SaveChangesAsync();
        await UpdateProductRating(productId);

        return Ok(new
        {
            Message = "Review saved.",
            Review = new
            {
                review.Id,
                review.Rating,
                review.Comment,
                review.CreatedAt,
                Username = review.User?.Username ?? User.Identity?.Name ?? "Customer",
                Images = DeserializeReviewImages(review.Images)
            }
        });
    }

    private async Task UpdateProductRating(string productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return;

        var reviews = await _context.ProductReviews
            .Where(r => r.ProductId == productId)
            .ToListAsync();

        product.ReviewCount = reviews.Count;
        product.AverageRating = reviews.Count == 0 ? 0 : Math.Round((decimal)reviews.Average(r => r.Rating), 1);

        await _context.SaveChangesAsync();
    }

    private static string[] DeserializeReviewImages(string? images)
    {
        if (string.IsNullOrWhiteSpace(images))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(images) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

public class ReviewRequest
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
