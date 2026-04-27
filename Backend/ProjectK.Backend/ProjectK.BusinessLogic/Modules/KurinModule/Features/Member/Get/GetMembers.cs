using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
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
    public class GetMembers : IRequest<ServiceResult<IEnumerable<MemberResponse>>>
    {
        public Guid GroupKey { get; set; }
        public Guid KurinKey { get; set; }
        public GetMembers(Guid groupKey, Guid kurinKey)
        {
            GroupKey = groupKey;
            KurinKey = kurinKey;
        }
    }

    public class GetMembersHandler : IRequestHandler<GetMembers, ServiceResult<IEnumerable<MemberResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserContext _currentUserContext;

        public GetMembersHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserContext currentUserContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserContext = currentUserContext;
        }

        private async Task ScrubRestrictedDataAsync(IEnumerable<MemberResponse> responses, IEnumerable<MemberEntity> entities, CancellationToken ct)
        {
            bool isAdminOrManager = _currentUserContext.IsInRole(UserRole.Admin.ToClaimValue()) ||
                                   _currentUserContext.IsInRole(UserRole.Manager.ToClaimValue());

            var currentUserId = _currentUserContext.UserId;
            var assignments = _currentUserContext.IsInRole(UserRole.Mentor.ToClaimValue()) && currentUserId.HasValue
                ? await _unitOfWork.MentorAssignments.GetByMentorUserKeyAsync(currentUserId.Value, ct)
                : Enumerable.Empty<ProjectK.Common.Entities.KurinModule.MentorAssignment>();
            
            var assignedGroupKeys = assignments.Where(a => a.RevokedAtUtc == null).Select(a => a.GroupKey).ToHashSet();

            var entityDict = entities.ToDictionary(e => e.MemberKey);

            foreach (var response in responses)
            {
                if (entityDict.TryGetValue(response.MemberKey, out var entity))
                {
                    bool isOwner = entity.UserKey.HasValue && entity.UserKey == currentUserId;
                    bool isAssignedMentor = entity.GroupKey.HasValue && assignedGroupKeys.Contains(entity.GroupKey.Value);
                    
                    bool canViewPrivate = isOwner || isAdminOrManager || isAssignedMentor;
                    
                    if (!canViewPrivate)
                    {
                        response.Address = string.Empty;
                        response.School = string.Empty;
                    }
                }
            }
        }

        public async Task<ServiceResult<IEnumerable<MemberResponse>>> Handle(GetMembers request, CancellationToken cancellationToken)
        {
            IEnumerable<MemberEntity> members;

            if (request.KurinKey == Guid.Empty)
            {
                members = await _unitOfWork.Members.GetAllAsync(request.GroupKey, cancellationToken);
            }
            else if (request.GroupKey == Guid.Empty)
            {
                members = await _unitOfWork.Members.GetAllByKurinKeyAsync(request.KurinKey, cancellationToken);
            }
            else
            {
                return new ServiceResult<IEnumerable<MemberResponse>>(ResultType.BadRequest);
            }

            var response = _mapper.Map<IEnumerable<MemberResponse>>(members);
            await ScrubRestrictedDataAsync(response, members, cancellationToken);

            return new ServiceResult<IEnumerable<MemberResponse>>(ResultType.Success, response);
        }
    }
}
