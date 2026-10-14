using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Settings;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ApproveWaitlistEntry;

public class ApproveWaitlistEntryCommandHandler : IRequestHandler<ApproveWaitlistEntryCommand, ServiceResult<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<AppUser> _userManager;
    private readonly IAccountProvisioningService _accountProvisioning;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IMembershipDirectory _memberships;
    private readonly KurinNumberOptions _numbers;
    private readonly KraiOptions _krai;

    public ApproveWaitlistEntryCommandHandler(
        IUnitOfWork unitOfWork,
        UserManager<AppUser> userManager,
        IAccountProvisioningService accountProvisioning,
        IEmailService emailService,
        ICurrentUserContext currentUserContext,
        IMembershipDirectory memberships,
        IOptions<KurinNumberOptions> numbers,
        IOptions<KraiOptions> krai)
    {
        _numbers = numbers.Value;
        _krai = krai.Value;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _accountProvisioning = accountProvisioning;
        _emailService = emailService;
        _currentUserContext = currentUserContext;
        _memberships = memberships;
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

        // A leader's kurin opens with the number they claimed, or none when they lead a підготовчий
        // курінь. A claim outside the range (an early form let "0" through) is read the same way:
        // the number is theirs to set later in the kurin's settings.
        int? kurinNumber = null;
        if (entry.IsKurinLeaderCandidate
            && !entry.IsPreparatoryKurin
            && int.TryParse(entry.ClaimedKurinNameOrNumber?.Trim(), out var claimed)
            && _numbers.IsAllowed(claimed))
        {
            kurinNumber = claimed;
            if (await _unitOfWork.Kurins.ExistsAsync(_krai.Default, claimed, cancellationToken))
            {
                return ServiceResult<Guid>.Failure(ResultType.Conflict, "KurinNumberTaken", "A kurin with this number already exists.");
            }
        }

        // 2. Create the inactive account and its invitation
        var provisioned = await _accountProvisioning.ProvisionAsync(
            new AccountProvisioningRequest(
                entry.Email,
                entry.FirstName,
                entry.LastName,
                entry.WaitlistEntryKey),
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
            // The waitlist does not ask for a край yet, so the kurin opens in the instance's default
            // one; an admin moves it from the kurin list if it belongs elsewhere.
            var kurin = new Kurin(kurinNumber) { KraiCode = _krai.Default };
            _unitOfWork.Kurins.Create(kurin, cancellationToken);

            // The entry remembers which kurin it opened, so activation can seat the founder in it.
            // It used to be written on the account, which then looked like the place it belonged.
            entry.FoundedKurinKey = kurin.KurinKey;
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
