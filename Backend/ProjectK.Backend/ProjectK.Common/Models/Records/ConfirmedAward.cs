using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Records;

/// <summary>One confirmed відзначення: which one and when it was earned.</summary>
public sealed record ConfirmedAward(MemberAwardLevel Level, DateTime DateAcquired);
