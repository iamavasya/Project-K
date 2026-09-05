using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Services;

/// <inheritdoc />
public sealed class AccountProvisioningService : IAccountProvisioningService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public AccountProvisioningService(
        UserManager<AppUser> userManager,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
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

        var invitation = new Invitation
        {
            InvitationKey = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString("N"),
            WaitlistEntryKey = request.WaitlistEntryKey,
            TargetUserKey = user.Id,
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(OnboardingPolicy.InvitationLifetimeDays)
        };

        _unitOfWork.Invitations.Create(invitation, cancellationToken);

        return new ServiceResult<AccountProvisioningResult>(
            ResultType.Success,
            new AccountProvisioningResult(user.Id, invitation.InvitationKey, invitation.Token));
    }
}
