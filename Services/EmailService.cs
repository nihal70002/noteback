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
            var host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? _config["EmailSettings:SmtpHost"];
            var portString = Environment.GetEnvironmentVariable("SMTP_PORT") ?? _config["EmailSettings:SmtpPort"];
            var user = Environment.GetEnvironmentVariable("SMTP_EMAIL") ?? _config["EmailSettings:SmtpUser"];
            var pass = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? _config["EmailSettings:SmtpPass"];
            var fromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? _config["EmailSettings:FromName"] ?? "Papercues Support";

            // If the app is accidentally using the placeholder from appsettings.json, stop immediately
            if (user == "your-email@gmail.com" || string.IsNullOrEmpty(pass))
            {
                _logger.LogWarning("Email sending failed because SMTP credentials are not fully configured or are still using placeholders.");
                return;
            }

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
            smtp.Timeout = 10000; // 10 seconds timeout
            await smtp.ConnectAsync(host, port, SecureSocketOptions.Auto);
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
