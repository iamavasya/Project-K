using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Services;

/// <inheritdoc />
public sealed class AccountProvisioningService : IAccountProvisioningService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUserContext _currentUserContext;

    public AccountProvisioningService(
        UserManager<AppUser> userManager,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ICurrentUserContext currentUserContext)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _currentUserContext = currentUserContext;
    }

    public async Task<AccountAvailability> CheckAvailabilityAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return AccountAvailability.EmailTaken;
        }

        if (await _unitOfWork.WaitlistEntries.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return AccountAvailability.WaitlistPending;
        }

        return AccountAvailability.Available;
    }

    public async Task<ServiceResult<AccountProvisioningResult>> ProvisionAsync(
        AccountProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            KurinKey = request.KurinKey,
            OnboardingStatus = OnboardingStatus.PendingActivation,
            IsBetaParticipant = request.IsBetaParticipant
        };

        var created = await _userManager.CreateAsync(user);
        if (!created.Succeeded)
        {
            var errors = string.Join(", ", created.Errors.Select(error => error.Description));
            return ServiceResult<AccountProvisioningResult>.Failure(
                ResultType.BadRequest,
                "UserNotCreated",
                $"Failed to create user: {errors}");
        }

        var waitlistEntryKey = request.WaitlistEntryKey ?? OpenApprovedWaitlistEntry(request, cancellationToken);

        var invitation = new Invitation
        {
            InvitationKey = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString("N"),
            WaitlistEntryKey = waitlistEntryKey,
            TargetUserKey = user.Id,
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(OnboardingPolicy.InvitationLifetimeDays)
        };

        _unitOfWork.Invitations.Create(invitation, cancellationToken);

        return new ServiceResult<AccountProvisioningResult>(
            ResultType.Success,
            new AccountProvisioningResult(user.Id, invitation.InvitationKey, invitation.Token));
    }

    /// <summary>
    /// Records the queue entry an invitation has to hang off for someone who never queued: a провід
    /// added them directly, so the entry is written already approved, by whoever is asking.
    /// </summary>
    private Guid OpenApprovedWaitlistEntry(AccountProvisioningRequest request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var entry = new WaitlistEntry
        {
            WaitlistEntryKey = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber ?? string.Empty,
            DateOfBirth = (request.DateOfBirth ?? DateOnly.FromDateTime(now)).ToDateTime(TimeOnly.MinValue),
            IsKurinLeaderCandidate = false,
            VerificationStatus = WaitlistVerificationStatus.ApprovedForInvitation,
            IsBetaParticipant = request.IsBetaParticipant,
            RequestedAtUtc = now,
            ReviewedAtUtc = now,
            ApprovedAtUtc = now,
            ReviewedByUserKey = _currentUserContext.UserId,
            InvitationSentAtUtc = now
        };

        _unitOfWork.WaitlistEntries.Create(entry, cancellationToken);
        return entry.WaitlistEntryKey;
    }
}
