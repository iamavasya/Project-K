using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Records;
using GroupEntity = ProjectK.Common.Entities.KurinModule.Group;
using MemberEntity = ProjectK.Common.Entities.KurinModule.Member;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert
{
    public class UpsertMember : IRequest<ServiceResult<MemberResponse>>
    {
        public Guid MemberKey { get; set; }
        public Guid? UserKey { get; set; }
        public Guid? KurinKey { get; set; }
        public Guid? GroupKey { get; set; }
        public bool CreateUserAccount { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? School { get; set; }
        public ICollection<PlastLevelHistoryDto> PlastLevelHistories { get; set; } = [];
        public bool RemoveProfilePhoto { get; set; }
        public byte[]? BlobContent { get; set; }
        public string? BlobFileName { get; set; }
        public string? BlobContentType { get; set; }
    }

    public class UpsertMemberHandler : IRequestHandler<UpsertMember, ServiceResult<MemberResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly INotificationService _notificationService;

        public UpsertMemberHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IPhotoService photoService,
            UserManager<AppUser> userManager,
            IEmailService emailService,
            ICurrentUserContext currentUserContext,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _photoService = photoService;
            _userManager = userManager;
            _emailService = emailService;
            _currentUserContext = currentUserContext;
            _notificationService = notificationService;
        }

        private bool CanEditRestrictedFields()
        {
            return _currentUserContext.IsInRole(UserRole.Admin.ToClaimValue()) ||
                   _currentUserContext.IsInRole(UserRole.Manager.ToClaimValue()) ||
                   _currentUserContext.IsInRole(UserRole.Mentor.ToClaimValue());
        }

        private bool IsAdmin()
        {
            return _currentUserContext.IsInRole(UserRole.Admin.ToClaimValue());
        }

        private bool IsCurrentUserOwner(MemberEntity member)
        {
            return member.UserKey.HasValue &&
                   _currentUserContext.UserId.HasValue &&
                   member.UserKey.Value == _currentUserContext.UserId.Value;
        }

        public async Task<ServiceResult<MemberResponse>> Handle(UpsertMember request, CancellationToken cancellationToken)
        {
            var existing = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
            
            if (existing != null && !CanEditRestrictedFields())
            {
                request.GroupKey = existing.GroupKey;
                request.KurinKey = existing.KurinKey;
                request.CreateUserAccount = false;
            }

            GroupEntity? group = null;
            if (request.GroupKey.HasValue && request.GroupKey.Value != Guid.Empty)
            {
                group = await _unitOfWork.Groups.GetByKeyAsync(request.GroupKey.Value, cancellationToken);
            }

            bool isCreated = false;
            string? oldBlobName = null;
            var wasProfileVerifiedCurrent = existing?.ProfileVerificationStatus == MemberProfileVerificationStatus.VerifiedCurrent;

            if (request.CreateUserAccount)
            {
                if (existing?.UserKey.HasValue == true)
                {
                    return new ServiceResult<MemberResponse>(ResultType.Conflict);
                }

                var userByEmail = await _userManager.FindByEmailAsync(request.Email);
                if (userByEmail != null)
                {
                    return new ServiceResult<MemberResponse>(ResultType.Conflict);
                }

                var waitlistByEmail = await _unitOfWork.WaitlistEntries.GetByEmailAsync(request.Email, cancellationToken);
                if (waitlistByEmail != null)
                {
                    return new ServiceResult<MemberResponse>(ResultType.Conflict);
                }
            }

            if (group == null && (!request.KurinKey.HasValue || request.KurinKey.Value == Guid.Empty))
            {
                return new ServiceResult<MemberResponse>(ResultType.NotFound);
            }

            if (existing == null)
            {
                existing = _mapper.Map<MemberEntity>(request);
                existing.GroupKey = group?.GroupKey;
                existing.KurinKey = group?.KurinKey ?? request.KurinKey!.Value;
                existing.LatestPlastLevel = existing.PlastLevelHistory
                    .OrderByDescending(p => p.DateAchieved)
                    .FirstOrDefault()?.PlastLevel;

                _unitOfWork.Members.Create(existing, cancellationToken);
                isCreated = true;
            }
            else
            {
                var preserveLinkedUserEmail = false;
                string? linkedUserEmail = null;
                var shouldMarkProfileStale = existing.ProfileVerificationStatus == MemberProfileVerificationStatus.VerifiedCurrent
                    && HasSignificantProfileChange(request, existing, group);

                if (existing.UserKey.HasValue)
                {
                    var isCurrentUserOwner = IsCurrentUserOwner(existing);
                    var emailChanged = !string.Equals(existing.Email, request.Email, StringComparison.OrdinalIgnoreCase);
                    var phoneChanged = !string.Equals(existing.PhoneNumber, request.PhoneNumber, StringComparison.OrdinalIgnoreCase);

                    if ((emailChanged || phoneChanged) && !CanEditRestrictedFields() && !isCurrentUserOwner)
                    {
                        return ServiceResult<MemberResponse>.Failure(
                            ResultType.BadRequest, 
                            "ContactInfoLinked", 
                            "Cannot change email or phone number for a member linked to an active user account. The user must update this via their account settings.");
                    }

                    preserveLinkedUserEmail = emailChanged && !IsAdmin();
                    linkedUserEmail = preserveLinkedUserEmail ? existing.Email : null;
                }

                oldBlobName = existing.ProfilePhotoBlobName;
                _mapper.Map(request, existing);

                if (preserveLinkedUserEmail)
                {
                    existing.Email = linkedUserEmail!;
                }

                existing.GroupKey = group?.GroupKey;
                existing.KurinKey = group?.KurinKey ?? request.KurinKey!.Value;

                if (shouldMarkProfileStale)
                {
                    existing.ProfileVerificationStatus = MemberProfileVerificationStatus.VerifiedStale;
                }
                
                if (CanEditRestrictedFields())
                {
                    UpdatePlastLevelHistory(existing.MemberKey, request.PlastLevelHistories, existing.PlastLevelHistory);
                    existing.LatestPlastLevel = existing.PlastLevelHistory
                        .OrderByDescending(p => p.DateAchieved)
                        .FirstOrDefault()?.PlastLevel;
                }

                _unitOfWork.Members.Update(existing, cancellationToken);
            }

            if (request.BlobContent is { Length: > 0 } && !string.IsNullOrWhiteSpace(request.BlobFileName))
            {
                var upload = await _photoService.UploadPhotoAsync(request.BlobContent, request.BlobFileName, cancellationToken);
                existing.ProfilePhotoBlobName = upload.BlobName;
                MarkVerifiedProfileStaleAfterPhotoChange(existing, oldBlobName);
            }

            if (request.RemoveProfilePhoto && oldBlobName != null)
            {
                existing.ProfilePhotoBlobName = null;
                MarkVerifiedProfileStaleAfterPhotoChange(existing, oldBlobName);
                await _photoService.DeletePhotoAsync(oldBlobName, cancellationToken);
            }

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (changes <= 0)
            {
                return new ServiceResult<MemberResponse>(ResultType.InternalServerError);
            }

            if (request.CreateUserAccount)
            {
                var invitationToken = await ProvisionUserAccountAndInvitationAsync(existing, cancellationToken);
                await _emailService.SendInvitationEmailAsync(existing.Email, invitationToken, cancellationToken);
            }

            if (!isCreated && oldBlobName != null && oldBlobName != existing.ProfilePhotoBlobName)
            {
                await _photoService.DeletePhotoAsync(oldBlobName, cancellationToken);
            }

            if (!isCreated
                && wasProfileVerifiedCurrent
                && existing.ProfileVerificationStatus == MemberProfileVerificationStatus.VerifiedStale)
            {
                await NotifyProfileChangedAfterVerificationAsync(existing, cancellationToken);
            }

            var response = _mapper.Map<MemberResponse>(existing);

            return isCreated
                ? new ServiceResult<MemberResponse>(
                    ResultType.Created,
                    response,
                    CreatedAtActionName: "GetByKey",
                    CreatedAtRouteValues: new { memberKey = response.MemberKey })
                : new ServiceResult<MemberResponse>(ResultType.Success, response);
        }

        private async Task<string> ProvisionUserAccountAndInvitationAsync(MemberEntity member, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = member.Email,
                Email = member.Email,
                FirstName = member.FirstName,
                LastName = member.LastName,
                KurinKey = member.KurinKey,
                OnboardingStatus = OnboardingStatus.PendingActivation,
                IsBetaParticipant = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException("Failed to create user account for member.");
            }

            member.UserKey = user.Id;
            _unitOfWork.Members.Update(member, cancellationToken);

            var waitlistEntry = new WaitlistEntry
            {
                WaitlistEntryKey = Guid.NewGuid(),
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                PhoneNumber = member.PhoneNumber,
                DateOfBirth = member.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                IsKurinLeaderCandidate = false,
                VerificationStatus = WaitlistVerificationStatus.ApprovedForInvitation,
                IsBetaParticipant = true,
                RequestedAtUtc = now,
                ReviewedAtUtc = now,
                ApprovedAtUtc = now,
                ReviewedByUserKey = _currentUserContext.UserId,
                InvitationSentAtUtc = now
            };

            var invitation = new Invitation
            {
                InvitationKey = Guid.NewGuid(),
                Token = Guid.NewGuid().ToString("N"),
                WaitlistEntryKey = waitlistEntry.WaitlistEntryKey,
                TargetUserKey = user.Id,
                ExpiresAtUtc = now.AddDays(7)
            };

            _unitOfWork.WaitlistEntries.Create(waitlistEntry, cancellationToken);
            _unitOfWork.Invitations.Create(invitation, cancellationToken);

            var accountChanges = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (accountChanges <= 0)
            {
                throw new InvalidOperationException("Failed to persist invitation for member account.");
            }

            return invitation.Token;
        }

        private static void UpdatePlastLevelHistory(
            Guid memberKey,
            ICollection<PlastLevelHistoryDto> plastLevelHistoryDto,
            ICollection<PlastLevelHistory> plastLevelHistory)
        {
            if (plastLevelHistoryDto == null || !plastLevelHistoryDto.Any())
            {
                plastLevelHistory.Clear();
                return;
            }

            var dtoDict = plastLevelHistoryDto
                .Where(dto => dto.PlastLevelHistoryKey.HasValue && dto.PlastLevelHistoryKey != Guid.Empty)
                .ToDictionary(dto => dto.PlastLevelHistoryKey!.Value);

            var entitiesToDelete = plastLevelHistory
                .Where(e => !dtoDict.ContainsKey(e.PlastLevelHistoryKey))
                .ToList();

            foreach (var entity in entitiesToDelete)
            {
                plastLevelHistory.Remove(entity);
            }

            foreach (var dto in plastLevelHistoryDto)
            {
                if (!dto.PlastLevelHistoryKey.HasValue || dto.PlastLevelHistoryKey == Guid.Empty)
                {
                    var newHistory = new PlastLevelHistory
                    {
                        MemberKey = memberKey,
                        PlastLevel = dto.PlastLevel,
                        DateAchieved = dto.DateAchieved
                    };
                    plastLevelHistory.Add(newHistory);
                }
                else
                {
                    var existingHistory = plastLevelHistory.FirstOrDefault(e => e.PlastLevelHistoryKey == dto.PlastLevelHistoryKey);
                    if (existingHistory != null)
                    {
                        existingHistory.PlastLevel = dto.PlastLevel;
                        existingHistory.DateAchieved = dto.DateAchieved;
                    }
                }
            }
        }

        private bool HasSignificantProfileChange(UpsertMember request, MemberEntity existing, GroupEntity? targetGroup)
        {
            var targetGroupKey = targetGroup?.GroupKey;
            var targetKurinKey = targetGroup?.KurinKey ?? request.KurinKey;

            return !string.Equals(existing.FirstName, request.FirstName, StringComparison.Ordinal)
                   || !string.Equals(existing.MiddleName ?? string.Empty, request.MiddleName ?? string.Empty, StringComparison.Ordinal)
                   || !string.Equals(existing.LastName, request.LastName, StringComparison.Ordinal)
                   || !string.Equals(existing.Email, request.Email, StringComparison.OrdinalIgnoreCase)
                   || !string.Equals(existing.PhoneNumber, request.PhoneNumber, StringComparison.Ordinal)
                   || existing.DateOfBirth != request.DateOfBirth
                   || !string.Equals(existing.Address ?? string.Empty, request.Address ?? string.Empty, StringComparison.Ordinal)
                   || !string.Equals(existing.School ?? string.Empty, request.School ?? string.Empty, StringComparison.Ordinal)
                   || existing.GroupKey != targetGroupKey
                   || existing.KurinKey != targetKurinKey
                   || (CanEditRestrictedFields() && HasPlastLevelHistoryChange(request.PlastLevelHistories, existing.PlastLevelHistory));
        }

        private static bool HasPlastLevelHistoryChange(
            ICollection<PlastLevelHistoryDto> requested,
            ICollection<PlastLevelHistory> existing)
        {
            if (requested.Count != existing.Count)
            {
                return true;
            }

            var existingByKey = existing.ToDictionary(history => history.PlastLevelHistoryKey);
            foreach (var dto in requested)
            {
                if (!dto.PlastLevelHistoryKey.HasValue || dto.PlastLevelHistoryKey == Guid.Empty)
                {
                    return true;
                }

                if (!existingByKey.TryGetValue(dto.PlastLevelHistoryKey.Value, out var entity))
                {
                    return true;
                }

                if (entity.PlastLevel != dto.PlastLevel || entity.DateAchieved != dto.DateAchieved)
                {
                    return true;
                }
            }

            return false;
        }

        private static void MarkVerifiedProfileStaleAfterPhotoChange(MemberEntity member, string? previousBlobName)
        {
            if (member.ProfileVerificationStatus == MemberProfileVerificationStatus.VerifiedCurrent
                && !string.Equals(previousBlobName, member.ProfilePhotoBlobName, StringComparison.Ordinal))
            {
                member.ProfileVerificationStatus = MemberProfileVerificationStatus.VerifiedStale;
            }
        }

        private async Task NotifyProfileChangedAfterVerificationAsync(MemberEntity member, CancellationToken cancellationToken)
        {
            if (!member.UserKey.HasValue)
            {
                return;
            }

            await _notificationService.NotifyAsync(
                new NotificationRequest
                {
                    RecipientUserKey = member.UserKey.Value,
                    Type = AppNotificationType.MemberProfileChangedAfterVerification,
                    Severity = AppNotificationSeverity.Warn,
                    Title = "Профіль потребує повторної перевірки",
                    Body = "Після підтвердження профільні дані змінилися. Потрібно перевірити їх повторно.",
                    EntityType = "Member",
                    EntityKey = member.MemberKey,
                    Route = $"/member/{member.MemberKey}",
                    ActorUserKey = _currentUserContext.UserId,
                    DeduplicationKey = $"member-profile-stale:{member.MemberKey}"
                },
                cancellationToken);
        }
    }
}
