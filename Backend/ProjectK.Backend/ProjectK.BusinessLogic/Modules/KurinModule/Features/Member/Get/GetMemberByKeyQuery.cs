using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using MemberEntity = ProjectK.Common.Entities.KurinModule.Member;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get;

public class GetMemberByKeyQuery : IRequest<ServiceResult<MemberResponse>>
{
    public Guid MemberKey { get; set; }
    public GetMemberByKeyQuery(Guid memberKey)
    {
        MemberKey = memberKey;
    }
}

public class GetMemberByKeyQueryHandler : IRequestHandler<GetMemberByKeyQuery, ServiceResult<MemberResponse>>
{
    private readonly IMemberUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IResourceScopeReader _scopeReader;
    private readonly IMembershipRepository _memberships;

    public GetMemberByKeyQueryHandler(IMemberUnitOfWork unitOfWork, IMapper mapper, ICurrentUserContext currentUserContext, IResourceScopeReader scopeReader, IUnitOfWork kurinData)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserContext = currentUserContext;
        _scopeReader = scopeReader;
        _memberships = kurinData.Memberships;
    }

    private async Task ScrubRestrictedDataAsync(
        MemberResponse response,
        MemberEntity entity,
        Guid? theirGroupKey,
        CancellationToken ct)
    {
        bool isOwner = entity.UserKey.HasValue && entity.UserKey == _currentUserContext.UserId;
        bool canViewPrivate = isOwner || _currentUserContext.CanManageWholeKurin();

        var kurinKey = _currentUserContext.KurinKey;
        if (!canViewPrivate && _currentUserContext.CanLeadGroups()
            && _currentUserContext.UserId.HasValue && kurinKey.HasValue && theirGroupKey.HasValue)
        {
            var ledGroups = await _scopeReader.GetLedGroupKeysAsync(_currentUserContext.UserId.Value, kurinKey.Value, ct);
            canViewPrivate = ledGroups.Contains(theirGroupKey.Value);
        }

        // The code is not a detail about a person, it is the thing they hand over. Leadership of
        // their own kurin has no use for it — knowing it would let one kurin sign someone into
        // another without ever asking them — so nobody but the person themselves is told it.
        if (!isOwner)
        {
            response.PublicId = null;
        }

        if (!canViewPrivate)
        {
            response.Address = string.Empty;
            response.School = string.Empty;
        }
    }

    public async Task<ServiceResult<MemberResponse>> Handle(GetMemberByKeyQuery request, CancellationToken cancellationToken)
    {
        var member = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
        if (member is null)
        {
            return new ServiceResult<MemberResponse>(ResultType.NotFound);
        }

        // Where they stand comes from their membership; the record itself no longer says.
        var placement = await _memberships.GetActiveForMemberAsync(request.MemberKey, cancellationToken);
        var here = placement.FirstOrDefault(m => m.KurinKey == _currentUserContext.KurinKey)
            ?? placement.FirstOrDefault();

        var memberResponse = _mapper.Map<MemberResponse>(member);
        memberResponse.KurinKey = here?.KurinKey ?? Guid.Empty;
        memberResponse.GroupKey = here?.GroupKey ?? Guid.Empty;

        await ScrubRestrictedDataAsync(memberResponse, member, here?.GroupKey, cancellationToken);

        return new ServiceResult<MemberResponse>(ResultType.Success, memberResponse);
    }
}
