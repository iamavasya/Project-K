using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Probe.Get;

public sealed class GetProbeProgress : IRequest<ServiceResult<ProbeProgressResponse>>
{
    public GetProbeProgress(Guid memberKey, string probeId)
    {
        MemberKey = memberKey;
        ProbeId = probeId;
    }

    public Guid MemberKey { get; }
    public string ProbeId { get; }
}

public sealed class GetProbeProgressHandler : IRequestHandler<GetProbeProgress, ServiceResult<ProbeProgressResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProbeProgressHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<ProbeProgressResponse>> Handle(GetProbeProgress request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProbeId))
        {
            return new ServiceResult<ProbeProgressResponse>(ResultType.BadRequest);
        }

        var member = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
        if (member is null)
        {
            return new ServiceResult<ProbeProgressResponse>(ResultType.NotFound);
        }

        var progress = await _unitOfWork.ProbeProgresses
            .GetByMemberAndProbeIdAsync(request.MemberKey, request.ProbeId, cancellationToken);

        if (progress is null)
        {
            var notStarted = ProbeProgressResponse.CreateNotStarted(request.MemberKey, member.KurinKey, request.ProbeId.Trim());
            return new ServiceResult<ProbeProgressResponse>(ResultType.Success, notStarted);
        }

        return new ServiceResult<ProbeProgressResponse>(ResultType.Success, ProbeProgressResponse.FromEntity(progress));
    }
}
