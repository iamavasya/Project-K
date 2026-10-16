using System.ComponentModel.DataAnnotations;

namespace ProjectK.Common.Entities.KraiModule;

/// <summary>
/// A пластовий край. Every kurin belongs to one, and the край's profile decides what its kurins
/// see: which вмілості and проби, the currency, the time zone. One instance holds as many краї
/// as it needs.
/// </summary>
public class Krai
{
    public const string Ukraine = "ua";
    public const string Czechia = "cz";

    /// <summary>A short code, the same one the catalogue marks its entries with (<c>ua</c>, <c>cz</c>).</summary>
    [Key]
    [MaxLength(8)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<KraiProfileRevision> ProfileRevisions { get; set; } = new List<KraiProfileRevision>();
}
