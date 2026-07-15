using AutoMapper;
using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward
{
    public sealed class ReviewMemberAward : IRequest<ServiceResult<MemberAwardDto>>
    {
        public Guid MemberAwardKey { get; set; }
        public bool IsApproved { get; set; }
    }

    public sealed class ReviewMemberAwardHandler : IRequestHandler<ReviewMemberAward, ServiceResult<MemberAwardDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public ReviewMemberAwardHandler(
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

        public async Task<ServiceResult<MemberAwardDto>> Handle(ReviewMemberAward request, CancellationToken cancellationToken)
        {
            var award = await _unitOfWork.MemberAwards.GetByKeyAsync(request.MemberAwardKey, cancellationToken);
            if (award is null)
            {
                return new ServiceResult<MemberAwardDto>(ResultType.NotFound);
            }

            var fromStatus = award.Status;
            if (fromStatus != BadgeProgressStatus.Submitted)
            {
                return new ServiceResult<MemberAwardDto>(ResultType.Conflict);
            }

            award.Status = request.IsApproved ? BadgeProgressStatus.Confirmed : BadgeProgressStatus.Rejected;
            award.ReviewedAtUtc = DateTime.UtcNow;
            award.ReviewedByUserKey = _currentUserContext.UserId;
            award.UpdatedDate = DateTime.UtcNow;

            _unitOfWork.MemberAwards.Update(award);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await NotifyMemberOwnerAsync(award, request.IsApproved, cancellationToken);

            return new ServiceResult<MemberAwardDto>(ResultType.Success, _mapper.Map<MemberAwardDto>(award));
        }

        private async Task NotifyMemberOwnerAsync(
            Common.Entities.KurinModule.MemberAward award,
            bool isApproved,
            CancellationToken cancellationToken)
        {
            var member = await _unitOfWork.Members.GetByKeyAsync(award.MemberKey, cancellationToken);
            if (member?.UserKey is null)
            {
                return;
            }

            await _notificationService.NotifyAsync(
                new NotificationRequest
                {
                    RecipientUserKey = member.UserKey.Value,
                    Type = AppNotificationType.MemberAwardReviewed,
                    Severity = isApproved ? AppNotificationSeverity.Success : AppNotificationSeverity.Warn,
                    Title = isApproved ? "Відзначення затверджено" : "Відзначення не затверджено",
                    Body = isApproved
                        ? "Ваше відзначення затверджено."
                        : "Ваше відзначення не затверджено. Перегляньте зауваження.",
                    EntityType = "MemberAward",
                    EntityKey = award.MemberAwardKey,
                    Route = $"/member/{member.MemberKey}",
                    ActorUserKey = _currentUserContext.UserId,
                    DeduplicationKey = $"award-review:{award.MemberAwardKey}"
                },
                cancellationToken);
        }
    }
}
