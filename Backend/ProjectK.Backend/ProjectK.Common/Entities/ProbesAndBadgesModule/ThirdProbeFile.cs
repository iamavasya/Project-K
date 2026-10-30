using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Entities.ProbesAndBadgesModule;

/// <summary>
/// Everything of the third probe that is not a project: when it opened, who guided it, the opening
/// conference, the camp with its посвідка, the вмілість that covers тіловиховання and the пластове
/// резюме. One per person and probe; created on the first write.
/// </summary>
public class ThirdProbeFile : Entity
{
    public Guid ThirdProbeFileKey { get; set; } = Guid.NewGuid();
    public Guid MemberKey { get; set; }
    public string ProbeId { get; set; } = string.Empty;

    /// <summary>«Дата відкриття ІІІ проби»; when empty, the day the previous probe was closed.</summary>
    public DateOnly? OpenedOn { get; set; }

    /// <summary>«Наставники»: the Звʼязковий, the referent of the third probe and others.</summary>
    public string? Mentors { get; set; }

    /// <summary>The opening talk, «Що для мене означає бути пластуном».</summary>
    public DateOnly? ConferenceOn { get; set; }
    public string? ConferenceWith { get; set; }
    public DateTime? ConferenceConfirmedAtUtc { get; set; }
    public Guid? ConferenceConfirmedByUserKey { get; set; }
    public string? ConferenceConfirmedByName { get; set; }

    public ThirdProbeCampKind? CampKind { get; set; }
    public string? CampName { get; set; }
    public DateOnly? CampFrom { get; set; }
    public DateOnly? CampTo { get; set; }

    /// <summary>The посвідка: a picture or a PDF, kept alive by this column alone.</summary>
    public string? CampCertificateBlobName { get; set; }
    public string? CampCertificateContentType { get; set; }
    public string? CampCertificateFileName { get; set; }

    public DateTime? CampConfirmedAtUtc { get; set; }
    public Guid? CampConfirmedByUserKey { get; set; }
    public string? CampConfirmedByName { get; set; }

    /// <summary>The catalogue id of the вмілість that shows фізична вправність або тіловиховання.</summary>
    public string? PhysicalBadgeId { get; set; }

    /// <summary>«Пластове резюме», in Markdown without pictures.</summary>
    public string Resume { get; set; } = string.Empty;

    public Guid? UpdatedByUserKey { get; set; }
    public string? UpdatedByName { get; set; }
}
