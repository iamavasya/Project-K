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

        public UpsertMemberHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IPhotoService photoService,
            UserManager<AppUser> userManager,
            IEmailService emailService,
            ICurrentUserContext currentUserContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _photoService = photoService;
            _userManager = userManager;
            _emailService = emailService;
            _currentUserContext = currentUserContext;
        }

        private bool CanEditRestrictedFields()
        {
            return _currentUserContext.IsInRole(UserRole.Admin.ToClaimValue()) ||
                   _currentUserContext.IsInRole(UserRole.Manager.ToClaimValue()) ||
                   _currentUserContext.IsInRole(UserRole.Mentor.ToClaimValue());
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

            if (group != null)
            {
                if (existing == null)
                {
                    existing = _mapper.Map<MemberEntity>(request);
                    existing.KurinKey = group.KurinKey;
                    existing.LatestPlastLevel = existing.PlastLevelHistory
                        .OrderByDescending(p => p.DateAchieved)
                        .FirstOrDefault()?.PlastLevel;

                    _unitOfWork.Members.Create(existing, cancellationToken);
                    isCreated = true;
                }
                else
                {
                    oldBlobName = existing.ProfilePhotoBlobName;
                    _mapper.Map(request, existing);

                    existing.KurinKey = group.KurinKey;
                    if (CanEditRestrictedFields())
                    {
                        UpdatePlastLevelHistory(existing.MemberKey, request.PlastLevelHistories, existing.PlastLevelHistory);
                        existing.LatestPlastLevel = existing.PlastLevelHistory
                            .OrderByDescending(p => p.DateAchieved)
                            .FirstOrDefault()?.PlastLevel;
                    }

                    _unitOfWork.Members.Update(existing, cancellationToken);
                }
            }
            else if (request.KurinKey.HasValue && request.KurinKey.Value != Guid.Empty)
            {
                if (existing == null)
                {
                    existing = _mapper.Map<MemberEntity>(request);
                    existing.GroupKey = null;
                    existing.KurinKey = request.KurinKey.Value;
                    existing.LatestPlastLevel = existing.PlastLevelHistory
                        .OrderByDescending(p => p.DateAchieved)
                        .FirstOrDefault()?.PlastLevel;

                    _unitOfWork.Members.Create(existing, cancellationToken);
                    isCreated = true;
                }
                else
                {
                    oldBlobName = existing.ProfilePhotoBlobName;
                    _mapper.Map(request, existing);

                    existing.GroupKey = null;
                    existing.KurinKey = request.KurinKey.Value;
                    if (CanEditRestrictedFields())
                    {
                        UpdatePlastLevelHistory(existing.MemberKey, request.PlastLevelHistories, existing.PlastLevelHistory);
                        existing.LatestPlastLevel = existing.PlastLevelHistory
                            .OrderByDescending(p => p.DateAchieved)
                            .FirstOrDefault()?.PlastLevel;
                    }

                    _unitOfWork.Members.Update(existing, cancellationToken);
                }
            }
            else
            {
                return new ServiceResult<MemberResponse>(ResultType.NotFound);
            }

            if (request.BlobContent is { Length: > 0 } && !string.IsNullOrWhiteSpace(request.BlobFileName))
            {
                var upload = await _photoService.UploadPhotoAsync(request.BlobContent, request.BlobFileName, cancellationToken);
                existing.ProfilePhotoBlobName = upload.BlobName;
            }

            if (request.RemoveProfilePhoto && oldBlobName != null)
            {
                existing.ProfilePhotoBlobName = null;
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
    }
}
