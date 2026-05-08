using Note.Backend.Models;

namespace Note.Backend.Services;

public interface IAuthService
{
    Task<string> RegisterAsync(string username, string email, string password, string role = "User");
    Task<(string? Token, string? Error)> LoginAsync(string email, string password);
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<(bool Success, string Message, string? ResetUrl)> ForgotPasswordAsync(string email);
    Task<(bool Success, string Message)> ResetPasswordAsync(string token, string? email, string newPassword);
}
