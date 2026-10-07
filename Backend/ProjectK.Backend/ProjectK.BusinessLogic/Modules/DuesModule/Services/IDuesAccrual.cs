namespace ProjectK.BusinessLogic.Modules.DuesModule.Services;

/// <summary>
/// Writes the quarters of вкладка that have fallen due and are not charged yet. Idempotent: running it
/// twice charges nothing twice, so it is run before every read of a kurin's dues.
/// </summary>
public interface IDuesAccrual
{
    /// <summary>Charges every youth membership in a гурток of the kurin up to the current quarter.</summary>
    Task AccrueKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Charges one membership up to the current quarter in the гурток given — the one it is leaving,
    /// so the quarters spent there stay that гурток's.
    /// </summary>
    Task AccrueMembershipAsync(Guid kurinKey, Guid membershipKey, Guid groupKey, CancellationToken cancellationToken = default);
}
