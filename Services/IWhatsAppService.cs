namespace Note.Backend.Services;

public interface IWhatsAppService
{
    Task<(bool Success, string? MessageSid, string? ErrorMessage)> SendMessageAsync(string phone, string message);
}
