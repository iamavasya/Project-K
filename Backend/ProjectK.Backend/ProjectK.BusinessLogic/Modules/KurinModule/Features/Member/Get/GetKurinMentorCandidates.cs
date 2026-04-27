using AutoMapper;
using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get;

public record GetKurinMentorCandidates(Guid KurinKey) : IRequest<ServiceResult<IEnumerable<MemberLookupDto>>>;

public class GetKurinMentorCandidatesHandler : IRequestHandler<GetKurinMentorCandidates, ServiceResult<IEnumerable<MemberLookupDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetKurinMentorCandidatesHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<MemberLookupDto>>> Handle(GetKurinMentorCandidates request, CancellationToken cancellationToken)
    {
        var members = await _uow.Members.GetAllByKurinKeyAsync(request.KurinKey, cancellationToken);
        var linkedMembers = members.Where(m => m.UserKey.HasValue).ToList();
        var response = _mapper.Map<IEnumerable<MemberLookupDto>>(linkedMembers);
        return new ServiceResult<IEnumerable<MemberLookupDto>>(ResultType.Success, response);
    }
}
