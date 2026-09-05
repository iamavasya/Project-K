namespace ProjectK.Common.Models.Records;

/// <summary>
/// What another module is allowed to know about a person: enough to name them, place them and reach
/// their account. Everything else — the profile, the levels, the verification — stays inside the
/// member's own module and is read through its own use cases.
/// </summary>
public sealed record MemberSummary(
    Guid MemberKey,
    Guid? UserKey,
    Guid KurinKey,
    Guid? GroupKey,
    string FirstName,
    string LastName,
    string Email,
    string? ProfilePhotoBlobName)
{
    /// <summary>The person's name as it is shown, empty when neither part is set.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// The person behind a freshly activated account, as the account knows them. Used only when an
/// account arrives without a member record and one has to be opened for it.
/// </summary>
public sealed record MemberForAccount(
    Guid UserKey,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    DateOnly DateOfBirth,
    Guid KurinKey);
