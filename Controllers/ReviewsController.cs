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

    public ReviewsController(NoteDbContext context, ICloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
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
                Images = !string.IsNullOrEmpty(r.Images) ? JsonSerializer.Deserialize<string[]>(r.Images) : new string[0]
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpPost]
    [Authorize]
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
        var imageUrls = new List<string>();
        if (files != null && files.Count > 0)
        {
            foreach (var file in files.Take(3)) // Max 3 images
            {
                if (file.Length > 5 * 1024 * 1024) // 5MB per image
                {
                    return BadRequest(new { Message = $"Image {file.FileName} is too large. Maximum size is 5MB per image." });
                }

                if (!file.ContentType.StartsWith("image/"))
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
        }

        review.Images = imageUrls.Count > 0 ? JsonSerializer.Serialize(imageUrls) : string.Empty;

        await _context.SaveChangesAsync();
        await UpdateProductRating(productId);

        return Ok(new { Message = "Review saved." });
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
}

public class ReviewRequest
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
