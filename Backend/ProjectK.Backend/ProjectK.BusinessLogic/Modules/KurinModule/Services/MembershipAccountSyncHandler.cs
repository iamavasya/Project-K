using MediatR;
using ProjectK.BusinessLogic.Services.Events;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Events;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Services;

/// <summary>
/// Keeps the account key on a person's current memberships equal to the one on their record. The
/// copy is what lets an access decision be made from membership alone; if it went stale, someone
/// invited today would be treated as belonging to no kurin at all.
/// </summary>
public sealed class MembershipAccountSyncHandler : INotificationHandler<DomainEventNotification<MemberAccountLinked>>
{
    private readonly IUnitOfWork _unitOfWork;

    public MembershipAccountSyncHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task Handle(DomainEventNotification<MemberAccountLinked> notification, CancellationToken cancellationToken)
        => _unitOfWork.Memberships.SyncAccountAsync(
            notification.Event.MemberKey,
            notification.Event.UserKey,
            cancellationToken);
}
