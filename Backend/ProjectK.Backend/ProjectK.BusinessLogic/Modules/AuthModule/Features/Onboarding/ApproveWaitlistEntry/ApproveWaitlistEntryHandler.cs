using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ApproveWaitlistEntry
{
    public class ApproveWaitlistEntryHandler : IRequestHandler<ApproveWaitlistEntryCommand, ServiceResult<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IAccountProvisioningService _accountProvisioning;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly IConfiguration _configuration;

        public ApproveWaitlistEntryHandler(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IAccountProvisioningService accountProvisioning,
            IEmailService emailService,
            ICurrentUserContext currentUserContext,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _accountProvisioning = accountProvisioning;
            _emailService = emailService;
            _currentUserContext = currentUserContext;
            _configuration = configuration;
        }

        public async Task<ServiceResult<Guid>> Handle(ApproveWaitlistEntryCommand request, CancellationToken cancellationToken)
        {
            var entry = await _unitOfWork.WaitlistEntries.GetByKeyAsync(request.WaitlistEntryKey, cancellationToken);
            if (entry == null)
            {
                return ServiceResult<Guid>.Failure(ResultType.NotFound, "WaitlistEntryNotFound", "Waitlist entry not found.");
            }

            if (entry.VerificationStatus == WaitlistVerificationStatus.ApprovedForInvitation)
            {
                return ServiceResult<Guid>.Failure(ResultType.Conflict, "WaitlistEntryAlreadyApproved", "Waitlist entry is already approved.");
            }

            var isClosedBeta = _configuration.GetValue<bool>("Onboarding:IsClosedBeta", true);

            // 1. ZBT Cap Validation (Simplified for now)
            if (isClosedBeta && !entry.IsKurinLeaderCandidate && entry.ClaimedKurinNameOrNumber != null)
            {
                if (int.TryParse(entry.ClaimedKurinNameOrNumber, out int num))
                {
                    var existingKurin = await _unitOfWork.Kurins.GetByNumberAsync(num, cancellationToken);
                    if (existingKurin != null && existingKurin.IsZbtKurin)
                    {
                        var activeUsersCount = await _unitOfWork.Users
                            .CountActiveAsync(existingKurin.KurinKey, cancellationToken);

                        if (activeUsersCount >= existingKurin.ZbtUserCap)
                        {
                            return ServiceResult<Guid>.Failure(ResultType.BadRequest, "BetaCapReached", $"ZBT Cap reached for kurin {num}. Hard cap is {existingKurin.ZbtUserCap}.");
                        }
                    }
                }
            }

            // 2. Create the inactive account and its invitation
            var provisioned = await _accountProvisioning.ProvisionAsync(
                new AccountProvisioningRequest(
                    entry.Email,
                    entry.FirstName,
                    entry.LastName,
                    entry.WaitlistEntryKey,
                    KurinKey: null,
                    IsBetaParticipant: isClosedBeta),
                cancellationToken);

            if (provisioned.Type != ResultType.Success || provisioned.Data is null)
            {
                return ServiceResult<Guid>.Failure(
                    provisioned.Type,
                    provisioned.ErrorCode ?? "UserNotCreated",
                    provisioned.ErrorMessage ?? "Failed to create user.");
            }

            // 3. Create Kurin Placeholder if leader candidate
            if (entry.IsKurinLeaderCandidate)
            {
                // Parse kurin number from claim if possible, else use 0 for placeholder
                int.TryParse(entry.ClaimedKurinNameOrNumber, out int kurinNumber);
                var kurin = new Kurin(kurinNumber)
                {
                    IsZbtKurin = isClosedBeta,
                    ZbtUserCap = 15
                };
                _unitOfWork.Kurins.Create(kurin, cancellationToken);

                var user = await _userManager.FindByIdAsync(provisioned.Data.UserKey.ToString());
                if (user is not null)
                {
                    user.KurinKey = kurin.KurinKey;
                    await _userManager.UpdateAsync(user);
                }
            }

            // 4. Update Waitlist Entry
            entry.VerificationStatus = WaitlistVerificationStatus.ApprovedForInvitation;
            entry.ApprovedAtUtc = DateTime.UtcNow;
            entry.ReviewedAtUtc = DateTime.UtcNow;
            entry.ReviewedByUserKey = _currentUserContext.UserId;
            entry.InvitationSentAtUtc = DateTime.UtcNow;
            _unitOfWork.WaitlistEntries.Update(entry, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Send Invitation Email
            await _emailService.SendInvitationEmailAsync(entry.Email, provisioned.Data.InvitationToken, cancellationToken);

            return new ServiceResult<Guid>(ResultType.Success, provisioned.Data.InvitationKey);
        }
    }
}
