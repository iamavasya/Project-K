namespace ProjectK.Common.Entities.KraiModule;

/// <summary>
/// One saved version of a край's profile. Rows are only ever added: the profile in force is the
/// latest revision, and the older ones are its history and what a rollback copies from.
/// </summary>
public class KraiProfileRevision
{
    public Guid KraiProfileRevisionKey { get; set; } = Guid.NewGuid();
    public string KraiCode { get; set; } = string.Empty;

    /// <summary>1 for the first version of a край's profile, one more for each save after it.</summary>
    public int Revision { get; set; }

    /// <summary>The shape <see cref="Json"/> was written in, so a later Lilejka can read an older one.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>The profile itself. A section left out takes its value from code.</summary>
    public string Json { get; set; } = "{}";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserKey { get; set; }
    public string? Note { get; set; }

    public Krai? Krai { get; set; }
}
