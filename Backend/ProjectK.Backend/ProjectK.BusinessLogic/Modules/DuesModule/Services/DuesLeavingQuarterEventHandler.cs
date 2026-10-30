using MediatR;
using ProjectK.BusinessLogic.Services.Events;
using ProjectK.Common.Interfaces.Modules.DuesModule;
using ProjectK.Common.Interfaces.Modules.KraiModule;
using ProjectK.Common.Models.Events;

namespace ProjectK.BusinessLogic.Modules.DuesModule.Services;

/// <summary>
/// Takes back the charge for the quarter a person was released in. Paying наперед, that quarter was
/// charged the day it began; released at its start, the person does not take it, so it is not theirs
/// to pay. Whatever they already paid for it stays on the account as a surplus — money is never removed.
/// </summary>
public sealed class DuesLeavingQuarterEventHandler : INotificationHandler<DomainEventNotification<MembershipLeft>>
{
    private readonly IDuesUnitOfWork _dues;
    private readonly IKraiProfileProvider _profiles;

    public DuesLeavingQuarterEventHandler(IDuesUnitOfWork dues, IKraiProfileProvider profiles)
    {
        _dues = dues;
        _profiles = profiles;
    }

    public async Task Handle(DomainEventNotification<MembershipLeft> notification, CancellationToken cancellationToken)
    {
        var left = notification.Event;
        var krai = await _profiles.DuesKraiForKurinAsync(left.KurinKey, cancellationToken);
        await _dues.DuesCharges.DeleteFromQuarterAsync(left.MembershipKey, krai.QuarterOf(left.LeftAtUtc).Index, cancellationToken);
    }
}
