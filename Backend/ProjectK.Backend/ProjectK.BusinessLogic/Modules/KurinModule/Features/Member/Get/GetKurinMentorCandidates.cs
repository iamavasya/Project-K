using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get;

public record GetKurinMentorCandidates(Guid KurinKey) : IRequest<ServiceResult<IEnumerable<MemberLookupDto>>>;

public class GetKurinMentorCandidatesHandler : IRequestHandler<GetKurinMentorCandidates, ServiceResult<IEnumerable<MemberLookupDto>>>
{
    private readonly IMemberUnitOfWork _uow;

    public GetKurinMentorCandidatesHandler(IMemberUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ServiceResult<IEnumerable<MemberLookupDto>>> Handle(GetKurinMentorCandidates request, CancellationToken cancellationToken)
    {
        var response = await _uow.Members.GetMentorCandidatesLookupAsync(request.KurinKey, cancellationToken);
        return new ServiceResult<IEnumerable<MemberLookupDto>>(ResultType.Success, response);
    }
}
