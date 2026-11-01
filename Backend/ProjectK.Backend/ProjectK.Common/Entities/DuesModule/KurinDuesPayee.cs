namespace ProjectK.Common.Entities.DuesModule;

/// <summary>
/// Someone the kurin hands вкладка on to, named by the kurin itself — a булава, a fund, a старшина.
/// A стаття sends its money to one; the kurin's box keeps what is still owed to each.
/// <para>
/// Never deleted once named: статті and transfers point at it. A payee no longer used is archived.
/// </para>
/// </summary>
public class KurinDuesPayee : Entity
{
    public const int MaxNameLength = 60;

    public Guid KurinDuesPayeeKey { get; set; } = Guid.NewGuid();
    public Guid KurinKey { get; set; }
    /// <summary>Unique within the kurin, compared without regard to case.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// A structure of the край, such as a булава: a пільга of the край that lowers the край's part
    /// lowers this payee's share too.
    /// </summary>
    public bool IsKrai { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }

    public bool IsArchived => ArchivedAtUtc.HasValue;
}
