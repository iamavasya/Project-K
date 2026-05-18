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
        var memberLookupDto = (await _uow.Members.GetMentorCandidatesLookupAsync(request.kurinKey, cancellationToken))
            .Where(m => string.Equals(m.UserRole, Common.Models.Enums.UserRole.Manager.ToString(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(m.UserRole, Common.Models.Enums.UserRole.Mentor.ToString(), StringComparison.OrdinalIgnoreCase))
            .DistinctBy(m => m.MemberKey)
            .ToList();

        return new ServiceResult<IEnumerable<MemberLookupDto>>(Common.Models.Enums.ResultType.Success, memberLookupDto);
    }
}
