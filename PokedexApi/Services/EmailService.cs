using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace PokedexApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendVerificationEmail(string toEmail, string token)
        {
            var verificationUrl = $"{_configuration["AppUrl"]}/verify-email?token={token}";
            var message = $"Please verify your email by clicking: {verificationUrl}";

            // TODO: Implement actual email sending (using SendGrid, MailKit, etc.)
            _logger.LogInformation($"Verification email would be sent to {toEmail}: {message}");

            await Task.CompletedTask;
        }

        public async Task SendPasswordResetEmail(string toEmail, string token)
        {
            var resetUrl = $"{_configuration["AppUrl"]}/reset-password?token={token}";
            var message = $"Reset your password by clicking: {resetUrl}";

            // TODO: Implement actual email sending
            _logger.LogInformation($"Password reset email would be sent to {toEmail}: {message}");

            await Task.CompletedTask;
        }
    }
}