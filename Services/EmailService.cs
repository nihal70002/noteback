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
            var host = Environment.GetEnvironmentVariable("SMTP_HOST")
                       ?? _config["EmailSettings:SmtpHost"];

            var portString = Environment.GetEnvironmentVariable("SMTP_PORT")
                             ?? _config["EmailSettings:SmtpPort"];

            var user = Environment.GetEnvironmentVariable("SMTP_EMAIL")
                       ?? _config["EmailSettings:SmtpUser"];

            var pass = Environment.GetEnvironmentVariable("SMTP_PASSWORD")
                       ?? _config["EmailSettings:SmtpPass"];

            var fromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME")
                           ?? _config["EmailSettings:FromName"]
                           ?? "Papercues Support";

            // Validate SMTP config
            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(pass))
            {
                _logger.LogWarning("SMTP credentials are missing.");
                return;
            }

            // Prevent placeholder config usage
            if (user.Contains("your-email"))
            {
                _logger.LogWarning("SMTP is still using placeholder credentials.");
                return;
            }

            // Parse port safely
            if (!int.TryParse(portString, out int port))
            {
                port = 587;
            }

            _logger.LogInformation("Preparing password reset email...");

            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(fromName, user));

            email.To.Add(MailboxAddress.Parse(toEmail));

            email.Subject = subject;

            email.Body = new TextPart(TextFormat.Html)
            {
                Text = htmlMessage
            };

            using var smtp = new SmtpClient();

            // Disable unsupported auth methods sometimes causing issues
            smtp.AuthenticationMechanisms.Remove("XOAUTH2");

            // Increase timeout for Railway cold starts/network latency
            smtp.Timeout = 30000;

            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            _logger.LogInformation($"Connecting to SMTP server: {host}:{port}");

            await smtp.ConnectAsync(
    host,
    465,
    SecureSocketOptions.SslOnConnect
);

            _logger.LogInformation("SMTP connection successful.");

            await smtp.AuthenticateAsync(user, pass);

            _logger.LogInformation("SMTP authentication successful.");

            await smtp.SendAsync(email);

            _logger.LogInformation("Password reset email sent successfully.");

            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP EMAIL ERROR");
        }
    }
}