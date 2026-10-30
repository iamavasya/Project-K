namespace ProjectK.Common.Entities.ProbesAndBadgesModule;

/// <summary>
/// One project of the third probe, as the youth describes it: what it is, whether it is the
/// individual one, and who guides it from outside the kurin. The probe is the one at the third
/// place of the person's line, kept by its catalogue id so a switch of edition does not carry
/// the project over to a probe it was not written for.
/// </summary>
public class ProbeProject : Entity
{
    public Guid ProbeProjectKey { get; set; } = Guid.NewGuid();

    /// <summary>The person. A key, not a relationship: the member lives in another module.</summary>
    public Guid MemberKey { get; set; }
    public string ProbeId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>The programme asks for one individual project among the five.</summary>
    public bool IsIndividual { get; set; }

    /// <summary>
    /// Markdown, as the editor's delta turns into it. Inline pictures are written as
    /// <c>project-image:{key}</c> rather than an address: blob links are signed and expire.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>«Ментор з напряму»: may be someone outside Plast, without an account.</summary>
    public string? DirectionMentorName { get; set; }
    public string? DirectionMentorContact { get; set; }

    public Guid? UpdatedByUserKey { get; set; }
    public string? UpdatedByName { get; set; }

    public ICollection<ProbeProjectImage> Images { get; set; } = new List<ProbeProjectImage>();
}
