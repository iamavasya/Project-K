using AutoMapper;
using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Members;

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
        var leaderships = await _uow.Leaderships.GetAllByTypeAsync(Common.Models.Enums.LeadershipType.KV, request.kurinKey, cancellationToken);

        var members = leaderships
            .SelectMany(l => l.LeadershipHistories)
            .Select(h => h.Member)
            .Where(m => m != null)
            .DistinctBy(m => m.MemberKey);

        var memberLookupDto = _mapper.Map<IEnumerable<MemberLookupDto>>(members);

        return new ServiceResult<IEnumerable<MemberLookupDto>>(Common.Models.Enums.ResultType.Success, memberLookupDto);
    }
}