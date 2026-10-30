using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Interfaces.Modules.DuesModule;

/// <summary>
/// What the dues module will tell others about who has paid. The whole of the way in: nobody
/// outside reads a box or a balance, and a quarter counts as paid only by the ledger's own rules.
/// </summary>
public interface IDuesDirectory
{
    /// <summary>
    /// Every quarter already over that a membership of the kurin closed with nothing owed — paid in
    /// full by the end of it, surplus or not. The running quarter is never among them.
    /// </summary>
    Task<IReadOnlyCollection<PaidQuarterRecord>> GetPaidQuartersAsync(Guid kurinKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// What the given memberships of the kurin still owe it, all their accounts together — the
    /// running quarter included, since it is charged when it begins. Zero when they are square or
    /// ahead: a surplus on one account pays the debt on another, as on the person's card.
    /// </summary>
    Task<decimal> GetOwedAsync(Guid kurinKey, IReadOnlyCollection<Guid> membershipKeys, CancellationToken cancellationToken = default);
}
