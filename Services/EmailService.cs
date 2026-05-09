using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Note.Backend.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        try
        {
            var host = _config["EmailSettings:SmtpHost"] ?? Environment.GetEnvironmentVariable("EmailSettings__SmtpHost");
            var portString = _config["EmailSettings:SmtpPort"] ?? Environment.GetEnvironmentVariable("EmailSettings__SmtpPort") ?? "587";
            var user = _config["EmailSettings:SmtpUser"] ?? Environment.GetEnvironmentVariable("EmailSettings__SmtpUser");
            var pass = _config["EmailSettings:SmtpPass"] ?? Environment.GetEnvironmentVariable("EmailSettings__SmtpPass");
            var fromName = _config["EmailSettings:FromName"] ?? Environment.GetEnvironmentVariable("EmailSettings__FromName") ?? "Papercues Support";

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                _logger.LogWarning("Email sending failed because SMTP credentials are not fully configured.");
                return;
            }

            if (!int.TryParse(portString, out int port)) port = 587;

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(fromName, user));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(user, pass);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
            
            _logger.LogInformation($"Successfully sent password reset email to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {toEmail}");
            // We don't throw here to avoid exposing internal server errors to the user during auth flow.
        }
    }
}
