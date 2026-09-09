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
using Microsoft.AspNetCore.Identity;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ResendInvitation
{
    public class ResendInvitationHandler : IRequestHandler<ResendInvitationCommand, ServiceResult<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly TimeProvider _timeProvider;

        public ResendInvitationHandler(IUnitOfWork unitOfWork, IEmailService emailService, TimeProvider timeProvider)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _timeProvider = timeProvider;
        }

        public async Task<ServiceResult<Guid>> Handle(ResendInvitationCommand request, CancellationToken cancellationToken)
        {
            var entry = await _unitOfWork.WaitlistEntries.GetByKeyAsync(request.WaitlistEntryKey, cancellationToken);
            if (entry == null)
            {
                return ServiceResult<Guid>.Failure(ResultType.NotFound, "WaitlistEntryNotFound", "Waitlist entry not found.");
            }

            var invitation = await _unitOfWork.Invitations.GetActiveByWaitlistEntryKeyAsync(request.WaitlistEntryKey, cancellationToken);

            if (invitation == null)
            {
                return ServiceResult<Guid>.Failure(ResultType.BadRequest, "NoActiveInvitation", "No active invitation found for this entry.");
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
                ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7)
            };
            _unitOfWork.Invitations.Create(newInvitation, cancellationToken);

            entry.InvitationSentAtUtc = DateTime.UtcNow;
            _unitOfWork.WaitlistEntries.Update(entry, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _emailService.SendInvitationEmailAsync(entry.Email, newInvitation.Token, cancellationToken);

            return new ServiceResult<Guid>(ResultType.Success, newInvitation.InvitationKey);
        }
    }

    public sealed class ResendInvitationByEmailHandler : IRequestHandler<ResendInvitationByEmailCommand, ServiceResult<bool>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly TimeProvider _timeProvider;

        public ResendInvitationByEmailHandler(
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            TimeProvider timeProvider)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _timeProvider = timeProvider;
        }

        public async Task<ServiceResult<bool>> Handle(
            ResendInvitationByEmailCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null || user.OnboardingStatus != OnboardingStatus.PendingActivation)
            {
                return new ServiceResult<bool>(ResultType.Success, true);
            }

            var entry = await _unitOfWork.WaitlistEntries.GetByEmailAsync(request.Email, cancellationToken);
            if (entry is null)
            {
                return new ServiceResult<bool>(ResultType.Success, true);
            }

            var invitation = await _unitOfWork.Invitations.GetActiveByWaitlistEntryKeyAsync(
                entry.WaitlistEntryKey,
                cancellationToken);

            var newInvitation = new Invitation
            {
                InvitationKey = Guid.NewGuid(),
                Token = Guid.NewGuid().ToString("N"),
                WaitlistEntryKey = entry.WaitlistEntryKey,
                TargetUserKey = user.Id,
                ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7)
            };
            _unitOfWork.Invitations.Create(newInvitation, cancellationToken);
            entry.InvitationSentAtUtc = DateTime.UtcNow;
            _unitOfWork.WaitlistEntries.Update(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _emailService.SendInvitationEmailAsync(user.Email, newInvitation.Token, cancellationToken);

            if (invitation is not null)
            {
                invitation.IsRevoked = true;
                _unitOfWork.Invitations.Update(invitation, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new ServiceResult<bool>(ResultType.Success, true);
        }
    }
}
