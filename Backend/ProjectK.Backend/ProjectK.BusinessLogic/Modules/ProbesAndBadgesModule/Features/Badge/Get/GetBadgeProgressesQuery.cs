using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Get;

public sealed class GetBadgeProgressesQuery : IRequest<ServiceResult<IEnumerable<BadgeProgressResponse>>>
{
    public GetBadgeProgressesQuery(Guid memberKey)
    {
        MemberKey = memberKey;
    }

    public Guid MemberKey { get; }
}

public sealed class GetBadgeProgressesQueryHandler : IRequestHandler<GetBadgeProgressesQuery, ServiceResult<IEnumerable<BadgeProgressResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemberDirectory _members;

    public GetBadgeProgressesQueryHandler(IUnitOfWork unitOfWork, IMemberDirectory members)
    {
        _unitOfWork = unitOfWork;
        _members = members;
    }

    public async Task<ServiceResult<IEnumerable<BadgeProgressResponse>>> Handle(GetBadgeProgressesQuery request, CancellationToken cancellationToken)
    {
        if (!await _members.ExistsAsync(request.MemberKey, cancellationToken))
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
