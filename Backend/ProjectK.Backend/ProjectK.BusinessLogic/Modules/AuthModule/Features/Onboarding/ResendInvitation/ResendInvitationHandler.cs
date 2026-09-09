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
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<ResendInvitationByEmailHandler> _logger;

        public ResendInvitationByEmailHandler(
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            TimeProvider timeProvider,
            ILogger<ResendInvitationByEmailHandler> logger)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _timeProvider = timeProvider;
            _logger = logger;
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
            // The letter goes first, and nothing is written until it is away. A send that throws must
            // leave the account exactly as it was: the invitation they already hold keeps working, and
            // no second live token is left behind for the next resend to pick between.
            try
            {
                await _emailService.SendInvitationEmailAsync(user.Email!, newInvitation.Token, cancellationToken);
            }
            catch (Exception exception)
            {
                // Answering differently here would say the address is one we know. The caller is told
                // the same thing an unknown address is told, and the failure is left in the log.
                _logger.LogError(
                    exception,
                    "Could not send a replacement invitation; nothing was changed for this account.");

                return new ServiceResult<bool>(ResultType.Success, true);
            }

            // One save: a person is never left holding two live invitations.
            _unitOfWork.Invitations.Create(newInvitation, cancellationToken);
            if (invitation is not null)
            {
                invitation.IsRevoked = true;
                _unitOfWork.Invitations.Update(invitation, cancellationToken);
            }

            entry.InvitationSentAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            _unitOfWork.WaitlistEntries.Update(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResult<bool>(ResultType.Success, true);
        }
    }
}
