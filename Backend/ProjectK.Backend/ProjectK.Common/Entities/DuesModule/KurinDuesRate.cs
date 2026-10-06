namespace ProjectK.Common.Entities.DuesModule;

/// <summary>
/// The kurin's part of a quarterly вкладка, from one quarter until the next change. The станиця part
/// has a full and a пільгова amount; the пільга lowers that part only, so the kurin part is one number.
/// </summary>
public class KurinDuesRate : Entity
{
    public Guid KurinDuesRateKey { get; set; } = Guid.NewGuid();
    public Guid KurinKey { get; set; }

    /// <summary>The first quarter it applies to, as <c>DuesQuarter.Index</c>.</summary>
    public int FromQuarter { get; set; }

    public decimal StanytsiaFull { get; set; }
    public decimal StanytsiaReduced { get; set; }
    public decimal KurinShare { get; set; }

    public Guid? SetByUserKey { get; set; }
}
