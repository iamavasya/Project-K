using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Extensions;
using Member = ProjectK.Common.Entities.KurinModule.Member;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Get;

public sealed record GetBadgeReviewQueue(Guid KurinKey) : IRequest<ServiceResult<IEnumerable<BadgeProgressResponse>>>;

public sealed class GetBadgeReviewQueueHandler : IRequestHandler<GetBadgeReviewQueue, ServiceResult<IEnumerable<BadgeProgressResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IResourceScopeReader _scopeReader;

    public GetBadgeReviewQueueHandler(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext, IResourceScopeReader scopeReader)
    {
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
        _scopeReader = scopeReader;
    }

    public async Task<ServiceResult<IEnumerable<BadgeProgressResponse>>> Handle(GetBadgeReviewQueue request, CancellationToken cancellationToken)
    {
        var membersInKurin = await _unitOfWork.Members.GetAllByKurinKeyAsync(request.KurinKey, cancellationToken);
        var membersDict = (membersInKurin ?? Enumerable.Empty<Member>()).ToDictionary(m => m.MemberKey);

        IEnumerable<Guid>? allowedGroupKeys = null;
        if (!_currentUserContext.CanManageWholeKurin())
        {
            // Group leaders only review their led groups.
            if (_currentUserContext.UserId == null)
            {
                return new ServiceResult<IEnumerable<BadgeProgressResponse>>(ResultType.Unauthorized, null);
            }
            allowedGroupKeys = await _scopeReader.GetLedGroupKeysAsync(_currentUserContext.UserId.Value, request.KurinKey, cancellationToken);
        }

        var filteredMembers = allowedGroupKeys != null 
            ? membersDict.Values.Where(m => m.GroupKey.HasValue && allowedGroupKeys.Contains(m.GroupKey.Value))
            : membersDict.Values;

        var memberKeys = filteredMembers.Select(m => m.MemberKey).ToList();

        var progresses = await _unitOfWork.BadgeProgresses.GetByMemberKeysAsync(memberKeys, cancellationToken);

        var allProgresses = progresses
            .Where(p => p.Status == BadgeProgressStatus.Submitted)
            .Select(p => BadgeProgressResponse.FromEntity(p, membersDict[p.MemberKey]))
            .OrderByDescending(p => p.SubmittedAtUtc)
            .ToList();

        return new ServiceResult<IEnumerable<BadgeProgressResponse>>(ResultType.Success, allProgresses.AsEnumerable());
    }
}
