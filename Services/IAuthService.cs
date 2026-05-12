using Note.Backend.Models;

namespace Note.Backend.Services;

public interface IAuthService
{
    Task<string> RegisterAsync(string username, string phoneNumber, string password, string role = "User");
    Task<(string? Token, string? Error)> LoginAsync(string phoneNumber, string password);
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<(bool Success, string Message, string? ResetUrl)> ForgotPasswordAsync(string phoneNumber);
    Task<(bool Success, string Message)> ResetPasswordAsync(string token, string? phoneNumber, string newPassword);
}
