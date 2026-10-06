namespace ProjectK.Common.Entities.DuesModule;

/// <summary>
/// One quarter of вкладка owed by one membership, and the гурток the person was in when it fell due.
/// <para>
/// The amount is not stored — it comes from the rates when the ledger is read. What is stored is the
/// гурток: a membership keeps only its current one, and a debt's гурткова частина stays with the
/// гурток it arose in after the person moves.
/// </para>
/// </summary>
public class DuesCharge : Entity
{
    public Guid DuesChargeKey { get; set; } = Guid.NewGuid();
    public Guid KurinKey { get; set; }
    public Guid GroupKey { get; set; }
    public Guid MembershipKey { get; set; }

    /// <summary>As <c>DuesQuarter.Index</c>. One charge per membership per quarter.</summary>
    public int Quarter { get; set; }
}
