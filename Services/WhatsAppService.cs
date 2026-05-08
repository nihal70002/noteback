using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Note.Backend.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly ILogger<WhatsAppService> _logger;
    private readonly string? _accountSid;
    private readonly string? _authToken;
    private readonly string? _whatsAppFrom;

    public WhatsAppService(ILogger<WhatsAppService> logger)
    {
        _logger = logger;

        // Railway environment variables are read securely at runtime.
        _accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        _authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
        _whatsAppFrom = Environment.GetEnvironmentVariable("TWILIO_WHATSAPP_FROM");

        _logger.LogInformation(
            "Twilio config loaded - AccountSid: {HasAccountSid}, AuthToken: {HasAuthToken}, WhatsAppFrom: {HasFrom}",
            !string.IsNullOrWhiteSpace(_accountSid),
            !string.IsNullOrWhiteSpace(_authToken),
            !string.IsNullOrWhiteSpace(_whatsAppFrom));
    }

    public async Task<(bool Success, string? MessageSid, string? ErrorMessage)> SendMessageAsync(string phone, string message)
    {
        if (string.IsNullOrWhiteSpace(_accountSid) ||
            string.IsNullOrWhiteSpace(_authToken) ||
            string.IsNullOrWhiteSpace(_whatsAppFrom))
        {
            _logger.LogError("Twilio WhatsApp credentials are missing.");
            return (false, null, "Twilio WhatsApp credentials are not configured.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            return (false, null, "Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return (false, null, "Message text is required.");
        }

        var formattedTo = FormatWhatsAppNumber(phone);
        var formattedFrom = FormatWhatsAppNumber(_whatsAppFrom);

        try
        {
            TwilioClient.Init(_accountSid, _authToken);

            _logger.LogInformation("Sending WhatsApp message to {Phone}", formattedTo);

            var result = await MessageResource.CreateAsync(
                from: new PhoneNumber(formattedFrom),
                to: new PhoneNumber(formattedTo),
                body: message.Trim());

            _logger.LogInformation("WhatsApp message sent successfully. Sid: {Sid}", result.Sid);
            return (true, result.Sid, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message to {Phone}", formattedTo);
            return (false, null, ex.Message);
        }
    }

    private static string FormatWhatsAppNumber(string phone)
    {
        var cleaned = phone.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        if (cleaned.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
        {
            return cleaned;
        }

        if (!cleaned.StartsWith("+"))
        {
            cleaned = $"+{cleaned}";
        }

        return $"whatsapp:{cleaned}";
    }
}
