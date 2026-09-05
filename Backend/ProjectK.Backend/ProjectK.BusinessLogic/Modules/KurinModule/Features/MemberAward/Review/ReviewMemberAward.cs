using AutoMapper;
using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectK.Common.Models.Events;
using ProjectK.Common.Models.Dtos.KurinModule;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward.Review
{
    public sealed class ReviewMemberAward : IRequest<ServiceResult<MemberAwardDto>>
    {
        public Guid MemberAwardKey { get; set; }
        public bool IsApproved { get; set; }
    }

    public sealed class ReviewMemberAwardHandler : IRequestHandler<ReviewMemberAward, ServiceResult<MemberAwardDto>>
    {
        private readonly IMemberUnitOfWork _unitOfWork;
        private readonly IMemberDirectory _members;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly IDomainEventPublisher _events;
        private readonly IMapper _mapper;

        public ReviewMemberAwardHandler(
            IMemberUnitOfWork unitOfWork,
            IMemberDirectory members,
            ICurrentUserContext currentUserContext,
            IDomainEventPublisher events,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _members = members;
            _currentUserContext = currentUserContext;
            _events = events;
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
            var ownerUserKey = await _members.FindAccountKeyAsync(award.MemberKey, cancellationToken);
            if (ownerUserKey is null)
            {
                return;
            }

            await _events.PublishAsync(
                new MemberAwardReviewed(
                    award.MemberAwardKey,
                    award.MemberKey,
                    ownerUserKey.Value,
                    isApproved,
                    _currentUserContext.UserId),
                cancellationToken);
        }
    }
}
