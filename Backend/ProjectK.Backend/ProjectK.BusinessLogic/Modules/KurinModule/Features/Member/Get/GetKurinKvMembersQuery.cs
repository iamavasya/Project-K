using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get;

public record GetKurinKvMembersQuery(Guid kurinKey) : IRequest<ServiceResult<IEnumerable<MemberLookupDto>>>;

public class GetKurinKvMembersQueryHandler : IRequestHandler<GetKurinKvMembersQuery, ServiceResult<IEnumerable<MemberLookupDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    public GetKurinKvMembersQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<MemberLookupDto>>> Handle(GetKurinKvMembersQuery request, CancellationToken cancellationToken)
    {
        // One row per active КВ office (Зв'язковий / Впорядник / Інструктор), UserRole = office role name.
        var kvMembers = await _uow.Leaderships.GetOfficeMembersLookupAsync(request.kurinKey, LeadershipType.KV, cancellationToken);

        return new ServiceResult<IEnumerable<MemberLookupDto>>(ResultType.Success, kvMembers);
    }
}
