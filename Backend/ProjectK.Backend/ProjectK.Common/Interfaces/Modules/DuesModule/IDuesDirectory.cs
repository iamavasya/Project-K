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
}
