using ProjectK.Common.Entities.KurinModule.Agenda;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Models;

/// <summary>An event group as the picker and management page see it.</summary>
public record AgendaCategoryResponse
{
    public Guid AgendaCategoryKey { get; init; }
    public Guid KurinKey { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ColorHex { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int? Capacity { get; init; }
    public bool WaitlistEnabled { get; init; }
    public string? DefaultDescription { get; init; }
    public bool RsvpRequired { get; init; }
    public int? DefaultDurationMinutes { get; init; }
    public int? ReminderLeadMinutes { get; init; }
    public bool IsArchived { get; init; }

    public static AgendaCategoryResponse From(AgendaCategory c) => new()
    {
        AgendaCategoryKey = c.AgendaCategoryKey,
        KurinKey = c.KurinKey,
        Name = c.Name,
        ColorHex = c.ColorHex,
        Icon = c.Icon,
        Capacity = c.Capacity,
        WaitlistEnabled = c.WaitlistEnabled,
        DefaultDescription = c.DefaultDescription,
        RsvpRequired = c.RsvpRequired,
        DefaultDurationMinutes = c.DefaultDurationMinutes,
        ReminderLeadMinutes = c.ReminderLeadMinutes,
        IsArchived = c.IsArchived
    };
}
