using Microsoft.Extensions.Options;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Settings;
using Resend;

namespace ProjectK.Infrastructure.Services.EmailService
{
    public class ResendEmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly EmailSettings _settings;

        public ResendEmailService(IResend resend, IOptions<EmailSettings> settings)
        {
            _resend = resend;
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            var message = new EmailMessage();
            message.From = $"{_settings.FromName} <{_settings.FromEmail}>";
            message.To.Add(to);
            message.Subject = subject;
            message.HtmlBody = body;

            await _resend.EmailSendAsync(message, cancellationToken);
        }

        public async Task SendInvitationEmailAsync(string to, string token, CancellationToken cancellationToken = default)
        {
            var activationUrl = $"{_settings.BaseUrl}/activate/{token}";
            var subject = "Welcome to ProjectK - Your Invitation";
            var body = $@"
                <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2>Welcome to ProjectK!</h2>
                    <p>You have been invited to join ProjectK. To activate your account and set your password, please click the link below:</p>
                    <div style='margin: 30px 0;'>
                        <a href='{activationUrl}' style='background-color: #007bff; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Activate Account</a>
                    </div>
                    <p>If the button doesn't work, you can copy and paste this URL into your browser:</p>
                    <p>{activationUrl}</p>
                    <p>This invitation will expire in 30 days.</p>
                    <hr style='border: 0; border-top: 1px solid #eee; margin: 30px 0;'>
                    <p style='color: #888; font-size: 12px;'>If you didn't request this invitation, you can safely ignore this email.</p>
                </div>";

            await SendEmailAsync(to, subject, body, cancellationToken);
        }

        public async Task SendPasswordResetEmailAsync(string to, string token, CancellationToken cancellationToken = default)
        {
            var resetUrl = $"{_settings.BaseUrl}/reset-password?token={token}&email={to}";
            var subject = "ProjectK - Password Reset Request";
            var body = $@"
                <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2>Password Reset Request</h2>
                    <p>We received a request to reset your ProjectK password. Click the link below to choose a new password:</p>
                    <div style='margin: 30px 0;'>
                        <a href='{resetUrl}' style='background-color: #28a745; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Reset Password</a>
                    </div>
                    <p>If you didn't request this, no further action is required.</p>
                    <p>The link will remain active for a limited time.</p>
                    <hr style='border: 0; border-top: 1px solid #eee; margin: 30px 0;'>
                    <p style='color: #888; font-size: 12px;'>This is an automated message, please do not reply.</p>
                </div>";

            await SendEmailAsync(to, subject, body, cancellationToken);
        }
    }
}
