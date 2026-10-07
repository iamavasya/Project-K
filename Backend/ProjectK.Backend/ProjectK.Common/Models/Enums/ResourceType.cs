namespace ProjectK.Common.Models.Enums;

public enum ResourceType
{
    Group,
    Member,
    Kurin,
    PlanningSession,
    Leadership,
    MemberWarning,
    MemberAward,
    ProbeProgress,
    BadgeProgress,
    AgendaItem,

    /// <summary>A гурток's вкладка: its box, its operations, its rate. Keyed by the гурток.</summary>
    GroupDues,

    /// <summary>The kurin's own box and rates, and what гуртки hand up. Keyed by the kurin.</summary>
    KurinDues,

    /// <summary>A гурток's точкування: its sheets, its youths' points, what was given by hand. Keyed by the гурток.</summary>
    GroupScore,

    /// <summary>The kurin's точкування: the table of гуртки everyone sees, and its rules. Keyed by the kurin.</summary>
    KurinScore,

    /// <summary>What the КВ scores among themselves, out of the table and out of youths' sight. Keyed by the kurin.</summary>
    KurinScorePrivate
}
