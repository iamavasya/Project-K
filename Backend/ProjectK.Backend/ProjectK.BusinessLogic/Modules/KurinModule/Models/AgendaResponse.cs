using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Models;

/// <summary>
/// An agenda item as the calendar and board see it. <see cref="CanEdit"/> and
/// <see cref="CanChangeStatus"/> are resolved per viewer so the UI can enable drag/edit without a
/// second round-trip.
/// </summary>
public record AgendaItemResponse
{
    public Guid AgendaItemKey { get; set; }
    public Guid KurinKey { get; set; }
    public AgendaItemKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AgendaItemStatus Status { get; set; }
    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public bool IsAllDay { get; set; }
    public Guid CreatedByUserKey { get; set; }
    public string? CreatedByName { get; set; }
    public bool CanEdit { get; set; }
    public bool CanChangeStatus { get; set; }
    public List<AgendaAssignmentDto> Assignments { get; set; } = [];
}

public record AgendaAssignmentDto
{
    public AgendaTargetType TargetType { get; set; }
    public Guid TargetKey { get; set; }

    /// <summary>Human label for the target (kurin number, group name, member full name).</summary>
    public string? Label { get; set; }
}

/// <summary>
/// The "Assign for" tree already trimmed to what the current user may target: the whole kurin and any
/// group/member for managers, only assigned groups and their members for mentors and group leaders.
/// </summary>
public record AgendaAssignTargetsResponse
{
    public bool CanTargetKurin { get; set; }
    public Guid KurinKey { get; set; }
    public string KurinLabel { get; set; } = string.Empty;

    /// <summary>Kurin-level проводи the viewer may target: КВ and Курінний провід.</summary>
    public List<AgendaLeadershipTargetDto> KurinLeaderships { get; set; } = [];
    public List<AgendaGroupTargetDto> Groups { get; set; } = [];
}

public record AgendaGroupTargetDto
{
    public Guid GroupKey { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>False when the group is shown only as a container for its members (no group-level target right).</summary>
    public bool CanTargetGroup { get; set; }

    /// <summary>The group's Гуртковий провід, when it has an active office; null otherwise.</summary>
    public AgendaLeadershipTargetDto? Leadership { get; set; }
    public List<AgendaMemberTargetDto> Members { get; set; } = [];
}

public record AgendaLeadershipTargetDto
{
    public Guid LeadershipKey { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool CanTarget { get; set; }
}

public record AgendaMemberTargetDto
{
    public Guid MemberKey { get; set; }
    public string FullName { get; set; } = string.Empty;
}
