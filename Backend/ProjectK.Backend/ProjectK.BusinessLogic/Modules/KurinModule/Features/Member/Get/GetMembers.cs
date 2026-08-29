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
using ProjectK.Common.Models.Dtos.KurinModule;

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
        private readonly IResourceScopeReader _scopeReader;

        public GetMembersHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserContext currentUserContext, IResourceScopeReader scopeReader)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserContext = currentUserContext;
            _scopeReader = scopeReader;
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

        // Who may see Address/School: whole-kurin managers see everyone; a member sees
        // their own record; a group leader sees members in the groups they lead. The led
        // group set is the only extra query, and only for group leaders — it is passed into
        // the projection so restricted fields are masked in SQL rather than post-read.
        private async Task<MemberFieldVisibility> BuildFieldVisibilityAsync(CancellationToken ct)
        {
            bool canManageWholeKurin = _currentUserContext.CanManageWholeKurin();
            var currentUserId = _currentUserContext.UserId;
            var kurinKey = _currentUserContext.KurinKey;

            IReadOnlyCollection<Guid> visibleGroupKeys = Array.Empty<Guid>();
            if (!canManageWholeKurin && currentUserId.HasValue && kurinKey.HasValue &&
                _currentUserContext.CanLeadGroups())
            {
                visibleGroupKeys = await _scopeReader.GetLedGroupKeysAsync(currentUserId.Value, kurinKey.Value, ct);
            }

            return new MemberFieldVisibility(canManageWholeKurin, currentUserId, visibleGroupKeys);
        }
    }
}
