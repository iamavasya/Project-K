using MediatR;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding.Handlers
{
    public class ResendInvitationHandler : IRequestHandler<ResendInvitationCommand, ServiceResult<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public ResendInvitationHandler(IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<ServiceResult<Guid>> Handle(ResendInvitationCommand request, CancellationToken cancellationToken)
        {
            var entry = await _unitOfWork.WaitlistEntries.GetByKeyAsync(request.WaitlistEntryKey, cancellationToken);
            if (entry == null)
            {
                return new ServiceResult<Guid>(ResultType.NotFound, Guid.Empty, "Waitlist entry not found.");
            }

            var invitation = await _unitOfWork.Invitations.GetActiveByWaitlistEntryKeyAsync(request.WaitlistEntryKey, cancellationToken);

            if (invitation == null)
            {
                return new ServiceResult<Guid>(ResultType.BadRequest, Guid.Empty, "No active invitation found for this entry.");
            }

            // Revoke old invitation and create a new one to refresh the token and expiry
            invitation.IsRevoked = true;
            _unitOfWork.Invitations.Update(invitation, cancellationToken);

            var newInvitation = new Invitation
            {
                InvitationKey = Guid.NewGuid(),
                Token = Guid.NewGuid().ToString("N"),
                WaitlistEntryKey = entry.WaitlistEntryKey,
                TargetUserKey = invitation.TargetUserKey,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
            };
            _unitOfWork.Invitations.Create(newInvitation, cancellationToken);

            entry.InvitationSentAtUtc = DateTime.UtcNow;
            _unitOfWork.WaitlistEntries.Update(entry, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _emailService.SendInvitationEmailAsync(entry.Email, newInvitation.Token, cancellationToken);

            return new ServiceResult<Guid>(ResultType.Success, newInvitation.InvitationKey);
        }
    }
}
