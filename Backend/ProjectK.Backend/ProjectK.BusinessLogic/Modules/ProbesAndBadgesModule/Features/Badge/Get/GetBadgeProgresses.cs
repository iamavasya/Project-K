using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Get;

public sealed class GetBadgeProgresses : IRequest<ServiceResult<IEnumerable<BadgeProgressResponse>>>
{
    public GetBadgeProgresses(Guid memberKey)
    {
        MemberKey = memberKey;
    }

    public Guid MemberKey { get; }
}

public sealed class GetBadgeProgressesHandler : IRequestHandler<GetBadgeProgresses, ServiceResult<IEnumerable<BadgeProgressResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBadgeProgressesHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<IEnumerable<BadgeProgressResponse>>> Handle(GetBadgeProgresses request, CancellationToken cancellationToken)
    {
        var member = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
        if (member is null)
        {
            return new ServiceResult<IEnumerable<BadgeProgressResponse>>(ResultType.NotFound);
        }

        var progresses = await _unitOfWork.BadgeProgresses.GetByMemberKeyAsync(request.MemberKey, cancellationToken);
        var response = progresses
            .OrderByDescending(x => x.UpdatedDate)
            .Select(BadgeProgressResponse.FromEntity)
            .ToList();

        return new ServiceResult<IEnumerable<BadgeProgressResponse>>(ResultType.Success, response);
    }
}
