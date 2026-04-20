using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
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
    public class ApproveWaitlistEntryHandler : IRequestHandler<ApproveWaitlistEntryCommand, ServiceResult<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserContext _currentUserContext;

        public ApproveWaitlistEntryHandler(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IEmailService emailService,
            ICurrentUserContext currentUserContext)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailService = emailService;
            _currentUserContext = currentUserContext;
        }

        public async Task<ServiceResult<Guid>> Handle(ApproveWaitlistEntryCommand request, CancellationToken cancellationToken)
        {
            var entry = await _unitOfWork.WaitlistEntries.GetByKeyAsync(request.WaitlistEntryKey, cancellationToken);
            if (entry == null)
            {
                return new ServiceResult<Guid>(ResultType.NotFound, Guid.Empty, "Waitlist entry not found.");
            }

            if (entry.VerificationStatus == WaitlistVerificationStatus.ApprovedForInvitation)
            {
                return new ServiceResult<Guid>(ResultType.Conflict, Guid.Empty, "Waitlist entry is already approved.");
            }

            // 1. ZBT Cap Validation (Simplified for now)
            // In a real scenario, we'd check the target kurin's active user count.
            // For bootstrap, we're creating a NEW kurin, so cap is always 10 and current is 0.
            // But if they were joining an existing kurin, we'd check:
            if (!entry.IsKurinLeaderCandidate && entry.ClaimedKurinNameOrNumber != null)
            {
                if (int.TryParse(entry.ClaimedKurinNameOrNumber, out int num))
                {
                    var existingKurin = await _unitOfWork.Kurins.GetByNumberAsync(num, cancellationToken);
                    if (existingKurin != null && existingKurin.IsZbtKurin)
                    {
                        var activeUsersCount = await _userManager.Users
                            .CountAsync(u => u.KurinKey == existingKurin.KurinKey && u.OnboardingStatus == OnboardingStatus.Active, cancellationToken);
                        
                        if (activeUsersCount >= existingKurin.ZbtUserCap)
                        {
                            return new ServiceResult<Guid>(ResultType.BadRequest, Guid.Empty, $"ZBT Cap reached for kurin {num}. Hard cap is {existingKurin.ZbtUserCap}.");
                        }
                    }
                }
            }

            // 2. Create Inactive AppUser
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = entry.Email,
                Email = entry.Email,
                FirstName = entry.FirstName,
                LastName = entry.LastName,
                OnboardingStatus = OnboardingStatus.PendingActivation,
                IsBetaParticipant = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return new ServiceResult<Guid>(ResultType.BadRequest, Guid.Empty, $"Failed to create user: {errors}");
            }

            // 2. Create Kurin Placeholder if leader candidate
            if (entry.IsKurinLeaderCandidate)
            {
                // Parse kurin number from claim if possible, else use 0 for placeholder
                int.TryParse(entry.ClaimedKurinNameOrNumber, out int kurinNumber);
                var kurin = new Kurin(kurinNumber)
                {
                    IsZbtKurin = true,
                    ZbtUserCap = 10
                };
                _unitOfWork.Kurins.Create(kurin, cancellationToken);
                user.KurinKey = kurin.KurinKey;
                await _userManager.UpdateAsync(user);
            }

            // 3. Create Invitation
            var invitation = new Invitation
            {
                InvitationKey = Guid.NewGuid(),
                Token = Guid.NewGuid().ToString("N"), // Simple token for now
                WaitlistEntryKey = entry.WaitlistEntryKey,
                TargetUserKey = user.Id,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
            };
            _unitOfWork.Invitations.Create(invitation, cancellationToken);

            // 4. Update Waitlist Entry
            entry.VerificationStatus = WaitlistVerificationStatus.ApprovedForInvitation;
            entry.ApprovedAtUtc = DateTime.UtcNow;
            entry.ReviewedAtUtc = DateTime.UtcNow;
            entry.ReviewedByUserKey = _currentUserContext.UserId;
            entry.InvitationSentAtUtc = DateTime.UtcNow;
            _unitOfWork.WaitlistEntries.Update(entry, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Send Invitation Email
            await _emailService.SendInvitationEmailAsync(entry.Email, invitation.Token, cancellationToken);

            return new ServiceResult<Guid>(ResultType.Success, invitation.InvitationKey);
        }
    }
}
