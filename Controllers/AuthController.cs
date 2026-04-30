using Microsoft.AspNetCore.Mvc;
using Note.Backend.Services;
using System.Security.Claims;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var error = await _authService.RegisterAsync(request.Username, request.Email, request.Password, request.Role ?? "User");
        if (!string.IsNullOrEmpty(error))
        {
            return BadRequest(new { Message = error });
        }
        return Ok(new { Message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (token, error) = await _authService.LoginAsync(request.Email, request.Password);
        if (token == null)
        {
            return Unauthorized(new { Message = error ?? "Invalid credentials" });
        }
        return Ok(new { Token = token });
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var success = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        if (!success)
        {
            return BadRequest(new { Message = "Failed to change password. Current password may be incorrect." });
        }

        return Ok(new { Message = "Password changed successfully" });
    }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
