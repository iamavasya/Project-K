using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using MemberEntity = ProjectK.Common.Entities.KurinModule.Member;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.ProfileVerification
{
    public sealed record VerifyMemberProfile(Guid MemberKey, string? Note = null) : IRequest<ServiceResult<MemberResponse>>;

    public sealed record ResetMemberProfileVerification(Guid MemberKey) : IRequest<ServiceResult<MemberResponse>>;

    public sealed class VerifyMemberProfileHandler : IRequestHandler<VerifyMemberProfile, ServiceResult<MemberResponse>>
    {
        private readonly MemberProfileVerificationService _service;

        public VerifyMemberProfileHandler(MemberProfileVerificationService service)
        {
            _service = service;
        }

        public Task<ServiceResult<MemberResponse>> Handle(VerifyMemberProfile request, CancellationToken cancellationToken)
            => _service.VerifyAsync(request.MemberKey, request.Note, cancellationToken);
    }

    public sealed class ResetMemberProfileVerificationHandler : IRequestHandler<ResetMemberProfileVerification, ServiceResult<MemberResponse>>
    {
        private readonly MemberProfileVerificationService _service;

        public ResetMemberProfileVerificationHandler(MemberProfileVerificationService service)
        {
            _service = service;
        }

        public Task<ServiceResult<MemberResponse>> Handle(ResetMemberProfileVerification request, CancellationToken cancellationToken)
            => _service.ResetAsync(request.MemberKey, cancellationToken);
    }

    public sealed class MemberProfileVerificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public MemberProfileVerificationService(
            IUnitOfWork unitOfWork,
            ICurrentUserContext currentUserContext,
            INotificationService notificationService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserContext = currentUserContext;
            _notificationService = notificationService;
            _mapper = mapper;
        }

        public async Task<ServiceResult<MemberResponse>> VerifyAsync(
            Guid memberKey,
            string? note,
            CancellationToken cancellationToken)
        {
            var member = await _unitOfWork.Members.GetByKeyAsync(memberKey, cancellationToken);
            var validation = await ValidateAsync(member, cancellationToken);
            if (validation is not null)
            {
                return validation;
            }

            member!.ProfileVerificationStatus = MemberProfileVerificationStatus.VerifiedCurrent;
            member.ProfileVerifiedAtUtc = DateTime.UtcNow;
            member.ProfileVerifiedByUserKey = _currentUserContext.UserId;
            member.ProfileVerificationNote = NormalizeNote(note);
            member.UpdatedDate = DateTime.UtcNow;

            var result = await SaveAsync(member, cancellationToken);
            if (result.Type == ResultType.Success)
            {
                await NotifyProfileVerifiedAsync(member, cancellationToken);
            }

            return result;
        }

        public async Task<ServiceResult<MemberResponse>> ResetAsync(
            Guid memberKey,
            CancellationToken cancellationToken)
        {
            var member = await _unitOfWork.Members.GetByKeyAsync(memberKey, cancellationToken);
            var validation = await ValidateAsync(member, cancellationToken);
            if (validation is not null)
            {
                return validation;
            }

            member!.ProfileVerificationStatus = MemberProfileVerificationStatus.Unverified;
            member.ProfileVerifiedAtUtc = null;
            member.ProfileVerifiedByUserKey = null;
            member.ProfileVerificationNote = null;
            member.UpdatedDate = DateTime.UtcNow;

            return await SaveAsync(member, cancellationToken);
        }

        private async Task<ServiceResult<MemberResponse>?> ValidateAsync(MemberEntity? member, CancellationToken cancellationToken)
        {
            if (!_currentUserContext.UserId.HasValue)
            {
                return new ServiceResult<MemberResponse>(ResultType.Unauthorized);
            }

            if (member is null)
            {
                return new ServiceResult<MemberResponse>(ResultType.NotFound);
            }

            if (!member.Kurin.ProfileVerificationEnabled)
            {
                return ServiceResult<MemberResponse>.Failure(
                    ResultType.BadRequest,
                    "ProfileVerificationDisabled",
                    "Profile verification is disabled for this kurin.");
            }

            if (member.UserKey.HasValue && member.UserKey.Value == _currentUserContext.UserId.Value)
            {
                return new ServiceResult<MemberResponse>(ResultType.Forbidden);
            }

            if (await CanVerifyAsync(member, cancellationToken))
            {
                return null;
            }

            return new ServiceResult<MemberResponse>(ResultType.Forbidden);
        }

        private async Task<bool> CanVerifyAsync(MemberEntity member, CancellationToken cancellationToken)
        {
            if (_currentUserContext.IsInRole(UserRole.Admin.ToClaimValue()))
            {
                return true;
            }

            if (_currentUserContext.IsInRole(UserRole.Manager.ToClaimValue()))
            {
                return _currentUserContext.KurinKey.HasValue
                       && _currentUserContext.KurinKey.Value == member.KurinKey;
            }

            if (!_currentUserContext.IsInRole(UserRole.Mentor.ToClaimValue())
                || !member.GroupKey.HasValue)
            {
                return false;
            }

            var assignments = await _unitOfWork.MentorAssignments.GetByMentorUserKeyAsync(
                _currentUserContext.UserId!.Value,
                cancellationToken);

            return assignments.Any(assignment =>
                assignment.RevokedAtUtc is null
                && assignment.GroupKey == member.GroupKey.Value);
        }

        private async Task<ServiceResult<MemberResponse>> SaveAsync(MemberEntity member, CancellationToken cancellationToken)
        {
            _unitOfWork.Members.Update(member, cancellationToken);
            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (changes <= 0)
            {
                return new ServiceResult<MemberResponse>(ResultType.InternalServerError);
            }

            return new ServiceResult<MemberResponse>(ResultType.Success, _mapper.Map<MemberResponse>(member));
        }

        private async Task NotifyProfileVerifiedAsync(MemberEntity member, CancellationToken cancellationToken)
        {
            if (!member.UserKey.HasValue)
            {
                return;
            }

            await _notificationService.NotifyAsync(
                new NotificationRequest
                {
                    RecipientUserKey = member.UserKey.Value,
                    Type = AppNotificationType.MemberProfileVerified,
                    Severity = AppNotificationSeverity.Success,
                    Title = "Профільні дані підтверджено",
                    Body = "Ваші профільні дані підтверджено як актуальні.",
                    EntityType = "Member",
                    EntityKey = member.MemberKey,
                    Route = $"/member/{member.MemberKey}",
                    ActorUserKey = _currentUserContext.UserId,
                    DeduplicationKey = $"member-profile-verified:{member.MemberKey}"
                },
                cancellationToken);
        }

        private static string? NormalizeNote(string? note)
        {
            var trimmed = note?.Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }
    }
}
