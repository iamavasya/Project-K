using AutoMapper;
using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get;

public record GetKurinKvMembers(Guid kurinKey) : IRequest<ServiceResult<IEnumerable<MemberLookupDto>>>;

public class GetKurinKvMembersHandler : IRequestHandler<GetKurinKvMembers, ServiceResult<IEnumerable<MemberLookupDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    public GetKurinKvMembersHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<MemberLookupDto>>> Handle(GetKurinKvMembers request, CancellationToken cancellationToken)
    {
        // One row per active КВ office (Зв'язковий / Впорядник / Інструктор), UserRole = office role name.
        var kvMembers = await _uow.Leaderships.GetOfficeMembersLookupAsync(request.kurinKey, LeadershipType.KV, cancellationToken);

        return new ServiceResult<IEnumerable<MemberLookupDto>>(ResultType.Success, kvMembers);
    }
}
