using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<ServiceResult<IEnumerable<MemberResponse>>> Handle(GetMembers request, CancellationToken cancellationToken)
        {
            var visibility = await BuildFieldVisibilityAsync(cancellationToken);

            IEnumerable<MemberListItemDto> members;
            if (request.KurinKey == Guid.Empty)
            {
                members = await _unitOfWork.Members.GetListItemsByGroupKeyAsync(request.GroupKey, visibility, cancellationToken);
            }
            else if (request.GroupKey == Guid.Empty)
            {
                members = await _unitOfWork.Members.GetListItemsByKurinKeyAsync(request.KurinKey, visibility, cancellationToken);
            }
            else
            {
                return new ServiceResult<IEnumerable<MemberResponse>>(ResultType.BadRequest);
            }

            var response = _mapper.Map<IEnumerable<MemberResponse>>(members);
            return new ServiceResult<IEnumerable<MemberResponse>>(ResultType.Success, response);
        }

        // Who may see Address/School: admins and managers see everyone; a member sees
        // their own record; a mentor sees members in their assigned groups. The mentor
        // group set is the only extra query, and only for mentors — it is passed into
        // the projection so restricted fields are masked in SQL rather than post-read.
        private async Task<MemberFieldVisibility> BuildFieldVisibilityAsync(CancellationToken ct)
        {
            bool isAdminOrManager = _currentUserContext.IsInRole(UserRole.Admin.ToClaimValue()) ||
                                    _currentUserContext.IsInRole(UserRole.Manager.ToClaimValue());
            var currentUserId = _currentUserContext.UserId;

            IReadOnlyCollection<Guid> visibleGroupKeys = Array.Empty<Guid>();
            if (!isAdminOrManager && currentUserId.HasValue &&
                _currentUserContext.IsInRole(UserRole.Mentor.ToClaimValue()))
            {
                var assignments = await _unitOfWork.MentorAssignments.GetByMentorUserKeyAsync(currentUserId.Value, ct);
                visibleGroupKeys = assignments
                    .Where(a => a.RevokedAtUtc == null)
                    .Select(a => a.GroupKey)
                    .ToHashSet();
            }

            return new MemberFieldVisibility(isAdminOrManager, currentUserId, visibleGroupKeys);
        }
    }
}
