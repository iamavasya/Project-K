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
        private readonly IMapper _mapper;

        public ReviewMemberAwardHandler(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserContext = currentUserContext;
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

            return new ServiceResult<MemberAwardDto>(ResultType.Success, _mapper.Map<MemberAwardDto>(award));
        }
    }
}
