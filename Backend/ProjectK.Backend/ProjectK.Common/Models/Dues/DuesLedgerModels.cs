namespace ProjectK.Common.Models.Dues;

/// <summary>A quarter's вкладка, split by where the money goes. The край part is last so a kurin without one reads as before.</summary>
public sealed record DuesAmount(decimal Stanytsia, decimal Kurin, decimal Group, decimal Krai = 0)
{
    public static DuesAmount Zero { get; } = new(0, 0, 0);

    /// <summary>What goes to the kurin's own payees, apart from the станиця and the край.</summary>
    public DuesPayeeShares Payees { get; init; } = DuesPayeeShares.None;

    public decimal Total => Stanytsia + Krai + Payees.Total + Kurin + Group;

    /// <summary>What the гурток hands up to the kurin: everything but its own part.</summary>
    public decimal Up => Stanytsia + Krai + Payees.Total + Kurin;
}

/// <summary>
/// Amounts per payee of the kurin, in the order the статті name them. Compared by value, so a
/// <see cref="DuesAmount"/> holding it still is.
/// </summary>
public sealed class DuesPayeeShares : IEquatable<DuesPayeeShares>
{
    public static DuesPayeeShares None { get; } = new([]);

    public DuesPayeeShares(IEnumerable<(Guid PayeeKey, decimal Amount)> shares)
    {
        var merged = new List<(Guid PayeeKey, decimal Amount)>();
        foreach (var (key, amount) in shares)
        {
            var at = merged.FindIndex(s => s.PayeeKey == key);
            if (at < 0)
            {
                merged.Add((key, amount));
            }
            else
            {
                merged[at] = (key, merged[at].Amount + amount);
            }
        }

        Shares = merged;
    }

    public IReadOnlyList<(Guid PayeeKey, decimal Amount)> Shares { get; }

    public decimal Total => Shares.Sum(s => s.Amount);

    public decimal Of(Guid payeeKey) => Shares.FirstOrDefault(s => s.PayeeKey == payeeKey).Amount;

    /// <summary>Each payee's amount added up across many.</summary>
    public static DuesPayeeShares Sum(IEnumerable<DuesPayeeShares> many) => new(many.SelectMany(m => m.Shares));

    public bool Equals(DuesPayeeShares? other) => other is not null && Shares.SequenceEqual(other.Shares);

    public override bool Equals(object? obj) => Equals(obj as DuesPayeeShares);

    public override int GetHashCode() => Shares.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.PayeeKey, s.Amount));
}

/// <summary>One quarter of one account: what fell due, what of it is paid, and with what пільга.</summary>
public sealed record DuesAccountQuarter(
    DuesQuarter Quarter,
    DuesAmount Charged,
    DuesAmount Paid,
    bool IsConcession)
{
    /// <summary>The tag whose rate priced the quarter; null for the kurin's rate for everyone.</summary>
    public Guid? RateTagKey { get; init; }

    /// <summary>The пільга of the край that set the quarter's amount; null when none of them did.</summary>
    public string? ConcessionName { get; init; }

    /// <summary>Zero when paid up, negative while owed. Never positive: a surplus is the account's.</summary>
    public decimal Balance => Paid.Total - Charged.Total;
}

/// <summary>
/// One membership's вкладка in one гурток. A person moved between гуртки has one per гурток: the
/// гурткова частина of an old debt stays with the гурток it arose in.
/// </summary>
public sealed record DuesAccount(
    Guid MembershipKey,
    Guid? GroupKey,
    IReadOnlyList<DuesAccountQuarter> Quarters,
    decimal Payments)
{
    public decimal Charged => Quarters.Sum(q => q.Charged.Total);

    /// <summary>Payments less charges: negative is debt, positive a surplus that pays what comes next.</summary>
    public decimal Balance => Payments - Charged;

    public DuesAmount Allocated => new(
        Quarters.Sum(q => q.Paid.Stanytsia),
        Quarters.Sum(q => q.Paid.Kurin),
        Quarters.Sum(q => q.Paid.Group),
        Quarters.Sum(q => q.Paid.Krai))
    {
        Payees = DuesPayeeShares.Sum(Quarters.Select(q => q.Paid.Payees))
    };
}

/// <summary>What is in a box, and how much of it is the box's own to spend.</summary>
public sealed record DuesCashBox(decimal Cash, decimal Card, decimal ToForward)
{
    public decimal Total => Cash + Card;

    /// <summary>
    /// What people have paid for quarters not charged yet, in a kurin's own box: it is theirs until the
    /// quarters come, and then partly the payees'.
    /// </summary>
    public decimal Ahead { get; init; }

    /// <summary>Total less what has to go up and what is paid ahead — the money the box may spend.</summary>
    public decimal Own => Total - ToForward - Ahead;
}

/// <summary>A гурток's box, and the transfers to the kurin that have not been confirmed yet.</summary>
public sealed record GroupDuesBox(Guid GroupKey, DuesCashBox Box, decimal InTransit);

/// <summary>How one гурток stands with the kurin: what it owes up, what it handed over, what arrived.</summary>
public sealed record GroupHandover(
    Guid GroupKey,
    decimal OwedUp,
    decimal Transferred,
    decimal Received)
{
    /// <summary>Collected for the станиця and the kurin but not handed over yet.</summary>
    public decimal Outstanding => OwedUp - Transferred;
}

/// <summary>
/// The kurin's own box and how each гурток stands with it. <see cref="DuesCashBox.ToForward"/> is
/// <see cref="ToStanytsia"/>, <see cref="ToKrai"/> and what is owed to <see cref="Payees"/> together.
/// </summary>
public sealed record KurinDuesBox(DuesCashBox Box, IReadOnlyList<GroupHandover> Handovers, decimal ToStanytsia = 0, decimal ToKrai = 0)
{
    /// <summary>Per payee of the kurin, what is collected for it and what has gone to it. Also within <see cref="DuesCashBox.ToForward"/>.</summary>
    public IReadOnlyList<PayeeHandover> Payees { get; init; } = [];
}

/// <summary>How the kurin stands with one of its payees.</summary>
public sealed record PayeeHandover(Guid PayeeKey, decimal Collected, decimal Sent)
{
    public decimal ToForward => Collected - Sent;
}
