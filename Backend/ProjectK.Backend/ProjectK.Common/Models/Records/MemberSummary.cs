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

/// <summary>
/// What a провід is shown before taking someone into their kurin: enough to be sure it is the right
/// person, and nothing more. Deliberately no kurins and no contact details — the code is looked up
/// by someone who was given it, and answering "which kurins is this person in" to anyone holding a
/// code would say more about them than they agreed to.
/// </summary>
public sealed record MemberCard(
    Guid MemberKey,
    string PublicId,
    string FirstName,
    string LastName,
    string? ProfilePhotoBlobName,
    int CurrentMembershipCount)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// Just enough of a person to tell whether a row of someone else's roster is already them. Not a
/// summary and not a card: this exists for matching, and every field on it is one people are matched
/// by.
/// </summary>
public sealed record MemberIdentity(
    Guid MemberKey,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateOnly DateOfBirth);
