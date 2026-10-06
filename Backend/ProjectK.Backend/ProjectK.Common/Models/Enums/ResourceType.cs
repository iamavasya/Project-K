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
    KurinDues
}
