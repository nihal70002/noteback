using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Note.Backend.Data;
using Note.Backend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Note.Backend.Services;

public class AuthService : IAuthService
{
    private readonly NoteDbContext _context;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;

    public AuthService(NoteDbContext context, IConfiguration config, IEmailService emailService)
    {
        _context = context;
        _config = config;
        _emailService = emailService;
    }

    public async Task<string> RegisterAsync(string username, string phoneNumber, string password, string role = "User")
    {
        // Phone number validation
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return "Phone number is required";
        }
        
        phoneNumber = phoneNumber.Trim();
        
        if (phoneNumber.Length < 10)
        {
            return "Phone number must be at least 10 digits";
        }
        
        if (await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber))
        {
            return "Phone number already exists";
        }

        var user = new User
        {
            PhoneNumber = phoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return string.Empty; // Success
    }

    public async Task<(string? Token, string? Error)> LoginAsync(string phoneNumber, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return (null, "Invalid credentials");
        }

        if (user.IsBlocked)
        {
            return (null, "Your account has been blocked. Please contact support.");
        }

        return (GenerateJwtToken(user), null);
    }

    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "default_super_secret_key_needs_to_be_long"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim("phoneNumber", user.PhoneNumber),
            new Claim(ClaimTypes.Name, user.PhoneNumber),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("Role", user.Role) // For easy access if needed
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string Message, string? ResetUrl)> ForgotPasswordAsync(string phoneNumber)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null) 
        {
            // Security: Always return success to prevent phone enumeration
            return (true, "If an account exists, password reset instructions have been sent.", null);
        }

        var rawToken = Guid.NewGuid().ToString("N");
        user.PasswordResetTokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken);
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        var resetUrl = $"https://papercues.in/reset-password?token={rawToken}&phoneNumber={Uri.EscapeDataString(phoneNumber)}";
        
        var emailSubject = "Reset Your Password - Papercues";
        var emailBody = $@"
            <h2>Password Reset Request</h2>
            <p>You requested to reset your password. Click the link below to set a new password:</p>
            <p><a href='{resetUrl}'>Reset Password</a></p>
            <p>If you did not request this, please ignore this email.</p>
            <p>This link will expire in 1 hour.</p>
        ";
        
        // Send email in background so user doesn't wait for SMTP connection
        _ = Task.Run(() => _emailService.SendEmailAsync(phoneNumber, emailSubject, emailBody));

        return (true, "If an account exists, password reset instructions have been sent.", null);
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string? phoneNumber, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
        {
            return (false, "Invalid request.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null) 
        {
            return (false, "Invalid request.");
        }

        if (string.IsNullOrEmpty(user.PasswordResetTokenHash) || user.PasswordResetTokenExpiresAt == null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
        {
            return (false, "Reset token has expired.");
        }

        if (!BCrypt.Net.BCrypt.Verify(token, user.PasswordResetTokenHash))
        {
            return (false, "Invalid reset token.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
        await _context.SaveChangesAsync();

        return (true, "Password reset successfully.");
    }
}
