namespace ProjectK.Common.Interfaces;

/// <summary>
/// Something that happened in the domain, stated as a fact and carrying everything a listener needs
/// to react to it. An event never says what should be done about it — that is the listener's call.
/// </summary>
public interface IDomainEvent
{
}

/// <summary>
/// How a module announces what happened without knowing who cares. Today the announcement is
/// delivered in the same process and inside the same request; the contract says nothing about that,
/// so the delivery can become a message broker without any publisher changing.
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}
