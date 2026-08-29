using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Dtos.KurinModule;

/// <summary>
/// One selected "Assign for" target. TargetKey is a KurinKey, GroupKey or MemberKey per TargetType.
/// </summary>
public sealed class AgendaTargetInput
{
    public AgendaTargetType TargetType { get; set; }
    public Guid TargetKey { get; set; }
}
