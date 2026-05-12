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
        var error = await _authService.RegisterAsync("", request.PhoneNumber, request.Password, request.Role ?? "User");
        if (!string.IsNullOrEmpty(error))
        {
            return BadRequest(new { Message = error });
        }
        return Ok(new { Message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (token, error) = await _authService.LoginAsync(request.PhoneNumber, request.Password);
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var (success, message, _) = await _authService.ForgotPasswordAsync(request.Email);
        
        // Always return Ok to prevent enumeration
        return Ok(new { Message = message });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var (success, message) = await _authService.ResetPasswordAsync(request.Token, request.Email, request.NewPassword);
        if (!success)
        {
            return BadRequest(new { Message = message });
        }
        return Ok(new { Message = message });
    }
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class LoginRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
