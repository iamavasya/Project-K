using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemberEntity = ProjectK.Common.Entities.KurinModule.Member;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get
{
    public class GetMemberByKey : IRequest<ServiceResult<MemberResponse>>
    {
        public Guid MemberKey { get; set; }
        public GetMemberByKey(Guid memberKey)
        {
            MemberKey = memberKey;
        }
    }

    public class GetMemberByKeyHandler : IRequestHandler<GetMemberByKey, ServiceResult<MemberResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserContext _currentUserContext;

        public GetMemberByKeyHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserContext currentUserContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserContext = currentUserContext;
        }

        private async Task ScrubRestrictedDataAsync(MemberResponse response, MemberEntity entity, CancellationToken ct)
        {
            bool isOwner = entity.UserKey.HasValue && entity.UserKey == _currentUserContext.UserId;
            bool isAdminOrManager = _currentUserContext.IsInRole(UserRole.Admin.ToClaimValue()) ||
                                   _currentUserContext.IsInRole(UserRole.Manager.ToClaimValue());
            
            bool canViewPrivate = isOwner || isAdminOrManager;

            if (!canViewPrivate && _currentUserContext.IsInRole(UserRole.Mentor.ToClaimValue()))
            {
                var assignments = await _unitOfWork.MentorAssignments.GetByMentorUserKeyAsync(_currentUserContext.UserId!.Value, ct);
                bool isAssignedMentor = assignments.Any(a => a.GroupKey == entity.GroupKey && a.RevokedAtUtc == null);
                canViewPrivate = isAssignedMentor;
            }

            if (!canViewPrivate)
            {
                response.Address = string.Empty;
                response.School = string.Empty;
            }
        }

        public async Task<ServiceResult<MemberResponse>> Handle(GetMemberByKey request, CancellationToken cancellationToken)
        {
            var member = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
            if (member is null)
            {
                return new ServiceResult<MemberResponse>(ResultType.NotFound);
            }
            var memberResponse = _mapper.Map<MemberResponse>(member);
            await ScrubRestrictedDataAsync(memberResponse, member, cancellationToken);

            return new ServiceResult<MemberResponse>(ResultType.Success, memberResponse);
        }
    }
}
