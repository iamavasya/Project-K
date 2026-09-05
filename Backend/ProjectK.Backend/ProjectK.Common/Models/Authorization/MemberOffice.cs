using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Authorization;

/// <summary>An office a member currently holds: the провід it belongs to and the role within it.</summary>
public sealed record MemberOffice(LeadershipType Type, LeadershipRole Role);
