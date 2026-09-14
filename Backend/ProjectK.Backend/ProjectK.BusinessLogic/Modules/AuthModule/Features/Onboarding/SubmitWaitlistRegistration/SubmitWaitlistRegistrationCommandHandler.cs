using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.SubmitWaitlistRegistration;

public class SubmitWaitlistRegistrationCommandHandler : IRequestHandler<SubmitWaitlistRegistrationCommand, ServiceResult<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemberDirectory _members;
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notifications;
    private readonly ILogger<SubmitWaitlistRegistrationCommandHandler> _logger;

    public SubmitWaitlistRegistrationCommandHandler(
        IUnitOfWork unitOfWork,
        IMemberDirectory members,
        UserManager<AppUser> userManager,
        IEmailService emailService,
        INotificationService notifications,
        ILogger<SubmitWaitlistRegistrationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _members = members;
        _userManager = userManager;
        _emailService = emailService;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<ServiceResult<Guid>> Handle(SubmitWaitlistRegistrationCommand request, CancellationToken cancellationToken)
    {
        // Input validation lives in SubmitWaitlistRegistrationCommandValidator (runs in the pipeline).
        var stanytsia = request.Stanytsia?.Trim();
        var regionOrCountry = request.RegionOrCountry?.Trim();
        var claimedKurinNumber = request.ClaimedKurinNameOrNumber?.Trim();

        // Check for existing waitlist entry with same email
        var existingEntry = await _unitOfWork.WaitlistEntries.GetByEmailAsync(request.Email, cancellationToken);
        if (existingEntry != null)
        {
            return ServiceResult<Guid>.Failure(ResultType.Conflict, "WaitlistEntryExists", "Waitlist entry with this email already exists.");
        }

        // Also check existing members just in case
        if (await _members.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return ServiceResult<Guid>.Failure(ResultType.Conflict, "MemberEmailExists", "A member with this email already exists in the system.");
        }

        var entry = new WaitlistEntry
        {
            WaitlistEntryKey = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            Stanytsia = stanytsia,
            RegionOrCountry = regionOrCountry,
            IsKurinLeaderCandidate = request.IsKurinLeaderCandidate,
            ClaimedKurinNameOrNumber = claimedKurinNumber,
            VerificationStatus = WaitlistVerificationStatus.Submitted,
            RequestedAtUtc = DateTime.UtcNow
        };

        _unitOfWork.WaitlistEntries.Create(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyAdministratorsAsync(entry, cancellationToken);

        return new ServiceResult<Guid>(ResultType.Created, entry.WaitlistEntryKey);
    }

    /// <summary>
    /// Every administrator hears about the entry twice: the bell in the app and a letter. Neither
    /// may fail the submission: the entry is already saved, and the applicant is not the one to
    /// tell about a mail outage.
    /// </summary>
    private async Task NotifyAdministratorsAsync(WaitlistEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var admins = await _userManager.GetUsersInRoleAsync(SystemRole.Admin);
            var applicant = $"{entry.FirstName} {entry.LastName}";
            var kurin = entry.ClaimedKurinNameOrNumber;

            await _notifications.NotifyManyAsync(
                admins.Select(admin => new NotificationRequest
                {
                    RecipientUserKey = admin.Id,
                    Type = AppNotificationType.WaitlistEntrySubmitted,
                    Title = "Нова заявка на розгляд",
                    Body = string.IsNullOrWhiteSpace(kurin) ? applicant : $"{applicant} · курінь {kurin}",
                    EntityType = "WaitlistEntry",
                    EntityKey = entry.WaitlistEntryKey,
                    Route = "/waitlist",
                    DeduplicationKey = $"waitlist-submitted:{entry.WaitlistEntryKey}:{admin.Id}"
                }),
                cancellationToken);

            foreach (var admin in admins.Where(admin => !string.IsNullOrWhiteSpace(admin.Email)))
            {
                await _emailService.SendWaitlistSubmittedEmailAsync(admin.Email!, applicant, kurin, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Waitlist entry {WaitlistEntryKey} saved, but administrators could not be told", entry.WaitlistEntryKey);
        }
    }
}
