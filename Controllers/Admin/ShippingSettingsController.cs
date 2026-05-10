using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;
using System.ComponentModel.DataAnnotations;

namespace Note.Backend.Controllers.Admin;

[ApiController]
[Route("api/admin/shipping-settings")]
[Authorize(Roles = "Admin")]
public class ShippingSettingsController : ControllerBase
{
    private readonly NoteDbContext _context;

    public ShippingSettingsController(NoteDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetShippingSettings()
    {
        var settings = await _context.ShippingSettings
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

        if (settings == null)
        {
            // Return default settings if none exist
            settings = new ShippingSettings();
        }

        return Ok(settings);
    }

    [HttpPost]
    public async Task<IActionResult> SaveShippingSettings([FromBody] ShippingSettings settings)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { Message = "Validation failed.", Errors = errors });
            }

            var existingSettings = await _context.ShippingSettings
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (existingSettings != null)
            {
                // Update existing settings
                existingSettings.Enabled = settings.Enabled;
                existingSettings.StandardShippingFee = settings.StandardShippingFee;
                existingSettings.FreeShippingThreshold = settings.FreeShippingThreshold;
                existingSettings.FreeShippingAmount = settings.FreeShippingAmount;
                existingSettings.FreeShippingType = settings.FreeShippingType;
                existingSettings.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new settings
                settings.Id = 0; // Let database generate ID
                settings.UpdatedAt = DateTime.UtcNow;
                _context.ShippingSettings.Add(settings);
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Shipping settings saved successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while saving shipping settings.", Error = ex.Message });
        }
    }
}
