using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Authorization;

/// <summary>Lightweight reference to a провід/КВ: its key, провід type and (for a гуртковий провід) its group.</summary>
public sealed record LeadershipRef(Guid LeadershipKey, LeadershipType Type, Guid? GroupKey);
