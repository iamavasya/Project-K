using MediatR;
using ProjectK.BusinessLogic.Services.Events;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Events;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Services;

/// <summary>
/// Closes the kurin's books on people who are gone. A membership names its person by a bare key —
/// that is what keeps the two tables independent — so nothing in the database follows them out, and
/// the rows left behind would go on placing someone who no longer exists.
/// </summary>
public sealed class MembershipCleanupHandler : INotificationHandler<DomainEventNotification<MembersRemoved>>
{
    private readonly IUnitOfWork _unitOfWork;

    public MembershipCleanupHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task Handle(DomainEventNotification<MembersRemoved> notification, CancellationToken cancellationToken)
        => _unitOfWork.Memberships.RemoveForMembersAsync(notification.Event.MemberKeys, cancellationToken);
}
