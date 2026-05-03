using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StorefrontController : ControllerBase
{
    private readonly NoteDbContext _context;

    public StorefrontController(NoteDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfig()
    {
        var config = await _context.StorefrontConfigs.FirstOrDefaultAsync();

        if (config == null)
        {
            // Return default config if none exists
            config = new StorefrontConfig
            {
                HeroImageUrl = "/product3.png",
                HeroTitle = "The Art of Logging",
                HeroSubtitle = "Capture<br />Every Moment",
                HeroLink = "/shop",
                Category1ImageUrl = "/product.png",
                Category1Title = "Daily Journals",
                Category1Link = "/shop",
                Category2ImageUrl = "/product5.png",
                Category2Title = "Goal Planners",
                Category2Link = "/shop"
            };
            _context.StorefrontConfigs.Add(config);
            await _context.SaveChangesAsync();
        }

        return Ok(config);
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateConfig([FromBody] StorefrontConfig newConfig)
    {
        var config = await _context.StorefrontConfigs.FirstOrDefaultAsync();

        if (config == null)
        {
            _context.StorefrontConfigs.Add(newConfig);
        }
        else
        {
            config.HeroImageUrl = newConfig.HeroImageUrl;
            config.HeroTitle = newConfig.HeroTitle;
            config.HeroSubtitle = newConfig.HeroSubtitle;
            config.HeroLink = newConfig.HeroLink;
            
            config.Category1ImageUrl = newConfig.Category1ImageUrl;
            config.Category1Title = newConfig.Category1Title;
            config.Category1Link = newConfig.Category1Link;
            
            config.Category2ImageUrl = newConfig.Category2ImageUrl;
            config.Category2Title = newConfig.Category2Title;
            config.Category2Link = newConfig.Category2Link;
        }

        await _context.SaveChangesAsync();
        return Ok(newConfig);
    }
}
