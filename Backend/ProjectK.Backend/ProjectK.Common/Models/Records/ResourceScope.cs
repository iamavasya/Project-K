namespace ProjectK.Common.Models.Records;

/// <summary>
/// The keys an authorization decision is made from. MemberUserKey is the user a member
/// record belongs to, used for the "own profile" rules.
/// </summary>
public sealed record ResourceScope(Guid KurinKey, Guid? GroupKey, Guid? MemberUserKey);
