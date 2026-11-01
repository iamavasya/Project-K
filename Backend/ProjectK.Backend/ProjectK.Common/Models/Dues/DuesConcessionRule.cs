using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.KraiProfiles;

namespace ProjectK.Common.Models.Dues;

/// <summary>
/// How a пільга of the край changes a quarter's full вкладка: a percent off, a sum off, or nothing
/// to pay, on the parts it names. The same rule is kept on the край's profile and copied onto each
/// person it is given to.
/// </summary>
public sealed record DuesConcessionRule(string Key, string Name, string Kind, decimal Value, DuesParts Parts)
{
    /// <summary>
    /// The quarter's вкладка with the пільга. A sum off is shared between the parts it names in
    /// proportion to them, so no part goes below zero and the cents add up. The kurin's payees that
    /// are structures of the край (<paramref name="kraiPayees"/>, such as a булава) count as the край's
    /// part; any other payee is left as it is.
    /// </summary>
    public DuesAmount Apply(DuesAmount full, IReadOnlySet<Guid>? kraiPayees = null)
    {
        var payees = full.Payees.Shares;
        var kraiCovered = Covers(DuesParts.Krai);
        // The parts in the order cuts are shared: станиця, край, the край's payees, курінь, гурток.
        var parts = new List<decimal>
        {
            Covers(DuesParts.Stanytsia) ? full.Stanytsia : 0,
            kraiCovered ? full.Krai : 0
        };
        parts.AddRange(payees.Select(s => kraiCovered && kraiPayees?.Contains(s.PayeeKey) == true ? s.Amount : 0));
        parts.Add(Covers(DuesParts.Kurin) ? full.Kurin : 0);
        parts.Add(Covers(DuesParts.Group) ? full.Group : 0);
        var covered = parts.Sum();
        if (covered <= 0)
        {
            return full;
        }

        var off = Kind switch
        {
            KraiDues.Exempt => covered,
            KraiDues.Amount => Math.Min(Value, covered),
            _ => Round(covered * Math.Clamp(Value, 0, 100) / 100)
        };

        // Shared out part by part; the last covered part takes what rounding left.
        var cuts = new decimal[parts.Count];
        var last = parts.FindLastIndex(part => part > 0);
        var given = 0m;
        for (var i = 0; i < parts.Count; i++)
        {
            if (parts[i] <= 0)
            {
                continue;
            }

            cuts[i] = Math.Min(parts[i], i == last ? off - given : Round(off * parts[i] / covered));
            given += cuts[i];
        }

        var payeeCut = cuts.Skip(2).Take(payees.Count).ToList();
        return new DuesAmount(
            full.Stanytsia - cuts[0],
            full.Kurin - cuts[^2],
            full.Group - cuts[^1],
            full.Krai - cuts[1])
        {
            Payees = new DuesPayeeShares(payees.Select((s, i) => (s.PayeeKey, s.Amount - payeeCut[i])))
        };
    }

    private bool Covers(DuesParts part) => (Parts & part) == part;

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>The parts named in a profile, as flags; a name this Lilejka does not know is left out.</summary>
    public static DuesParts PartsOf(IEnumerable<string>? names)
    {
        var parts = DuesParts.None;
        foreach (var name in names ?? [])
        {
            parts |= name?.Trim().ToLowerInvariant() switch
            {
                KraiDues.PartStanytsia => DuesParts.Stanytsia,
                KraiDues.PartKrai => DuesParts.Krai,
                KraiDues.PartKurin => DuesParts.Kurin,
                KraiDues.PartGroup => DuesParts.Group,
                _ => DuesParts.None
            };
        }

        return parts;
    }
}
