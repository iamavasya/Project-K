using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Dtos.ProbesAndBadgesModule.Requests;

/// <summary>A third-probe project, laid out as the УПЮ template asks.</summary>
public class SaveProbeProjectRequest
{
    public string? Title { get; set; }
    public ProbeProjectArea? Area { get; set; }
    public List<ProbeProjectReach>? Reach { get; set; }
    public bool IsShared { get; set; }
    public string? SharedWith { get; set; }
    public DateOnly? StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public string? Location { get; set; }
    public string? EventLeads { get; set; }
    public string? CoordinatorName { get; set; }
    public string? CoordinatorContact { get; set; }

    /// <summary>Markdown, like Plan and Results; pictures as <c>![…](project-image:{key})</c>.</summary>
    public string? Goal { get; set; }
    public string? Plan { get; set; }
    public string? Results { get; set; }
}

/// <summary>The third-probe file as the person writes it; confirmations go their own way.</summary>
public class SaveThirdProbeFileRequest
{
    public DateOnly? OpenedOn { get; set; }
    public string? Mentors { get; set; }
    public DateOnly? ConferenceOn { get; set; }
    public string? ConferenceWith { get; set; }
    public ThirdProbeCampKind? CampKind { get; set; }
    public string? CampName { get; set; }
    public DateOnly? CampFrom { get; set; }
    public DateOnly? CampTo { get; set; }
    public string? PhysicalBadgeId { get; set; }
    public string? Resume { get; set; }
}

/// <summary>A project marked done, with what its coordinator said.</summary>
public class CompleteProbeProjectRequest
{
    public string? CoordinatorComment { get; set; }
}
