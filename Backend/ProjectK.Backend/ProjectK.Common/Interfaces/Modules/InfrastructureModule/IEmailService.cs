using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
        Task SendInvitationEmailAsync(string to, string token, CancellationToken cancellationToken = default);
        Task SendPasswordResetEmailAsync(string to, string token, CancellationToken cancellationToken = default);
    }
}
