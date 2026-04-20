using Microsoft.Extensions.Logging;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Services
{
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;

        public MockEmailService(ILogger<MockEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[MOCK EMAIL] To: {To}, Subject: {Subject}, Body: {Body}", to, subject, body);
            return Task.CompletedTask;
        }

        public Task SendInvitationEmailAsync(string to, string token, CancellationToken cancellationToken = default)
        {
            var body = $"Your invitation token is: {token}";
            return SendEmailAsync(to, "ProjectK Invitation", body, cancellationToken);
        }

        public Task SendPasswordResetEmailAsync(string to, string token, CancellationToken cancellationToken = default)
        {
            var body = $"Your password reset token is: {token}";
            return SendEmailAsync(to, "ProjectK Password Reset", body, cancellationToken);
        }
    }
}
