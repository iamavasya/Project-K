using ProjectK.Common.Interfaces.Modules.DuesModule;
using ProjectK.Common.Models.Dues;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.DuesModule.Services;

/// <inheritdoc />
/// <remarks>
/// A quarter is paid when the ledger leaves it with no balance owed — and the ledger pays the oldest
/// quarter first, so a quarter paid is one every quarter before it is paid too. Charges are brought
/// up to date first, as every read of the box does.
/// </remarks>
public sealed class DuesDirectory : IDuesDirectory
{
    private readonly IDuesUnitOfWork _dues;
    private readonly IDuesAccrual _accrual;
    private readonly TimeProvider _time;

    public DuesDirectory(IDuesUnitOfWork dues, IDuesAccrual accrual, TimeProvider time)
    {
        _dues = dues;
        _accrual = accrual;
        _time = time;
    }

    public async Task<IReadOnlyCollection<PaidQuarterRecord>> GetPaidQuartersAsync(Guid kurinKey, CancellationToken cancellationToken = default)
    {
        await _accrual.AccrueKurinAsync(kurinKey, cancellationToken);

        var ledger = new DuesLedger(
            await _dues.KurinDuesRates.GetForKurinAsync(kurinKey, cancellationToken),
            await _dues.GroupDuesRates.GetForKurinAsync(kurinKey, cancellationToken),
            await _dues.DuesConcessions.GetForKurinAsync(kurinKey, cancellationToken),
            await _dues.DuesCharges.GetForKurinAsync(kurinKey, cancellationToken),
            await _dues.DuesEntries.GetForKurinAsync(kurinKey, cancellationToken));

        var current = DuesQuarter.Of(_time.GetUtcNow().UtcDateTime);
        return ledger.Accounts()
            .SelectMany(account => account.Quarters
                .Where(q => q.Quarter < current && q.Charged.Total > 0 && q.Balance >= 0)
                .Select(q => new PaidQuarterRecord(account.MembershipKey, q.Quarter.Index, q.Quarter.Next().FirstDay.AddDays(-1))))
            .Distinct()
            .ToList();
    }
}
