using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.DuesModule.Services;

/// <summary>
/// The гурток every dues request is about, checked to stand in the kurin the caller acts in. The
/// endpoint's authorization already pins both; this is the handler's own answer to "does it exist",
/// and it keeps the kurin key out of every request body.
/// </summary>
public sealed class GroupDuesAccess
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public GroupDuesAccess(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<(Group? Group, ServiceResult<T>? Failure)> OpenAsync<T>(Guid groupKey, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetByKeyAsync(groupKey, cancellationToken);
        if (group is null)
        {
            return (null, ServiceResult<T>.Failure(ResultType.NotFound, "GroupNotFound", "There is no such гурток."));
        }

        if (_currentUser.KurinKey != group.KurinKey)
        {
            return (null, new ServiceResult<T>(ResultType.Forbidden));
        }

        return (group, null);
    }
}
