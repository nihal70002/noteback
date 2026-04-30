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

    public AuthService(NoteDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<string> RegisterAsync(string username, string email, string password, string role = "User")
    {
        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            return "Email already exists";
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return string.Empty; // Success
    }

    public async Task<(string? Token, string? Error)> LoginAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
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
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
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
}
