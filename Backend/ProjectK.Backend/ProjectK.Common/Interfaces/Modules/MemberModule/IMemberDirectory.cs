using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Interfaces.Modules.MemberModule;

/// <summary>
/// Everything another module may ask of the member module. It is the only way in: no other module
/// holds <c>IMemberRepository</c> or names the <c>Member</c> entity, which is what makes the module
/// liftable later — every call here is one that could be answered over a network instead.
/// <para>
/// Reads that return many people do so in one call by design. A contract that could only answer
/// "one by key" would turn every list into N calls, and N round trips once the module moves out.
/// </para>
/// </summary>
public interface IMemberDirectory
{
    /// <summary>Whether such a member exists at all.</summary>
    Task<bool> ExistsAsync(Guid memberKey, CancellationToken cancellationToken = default);

    /// <summary>Whether this address already belongs to someone. Guards registration.</summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>The person, or null when there is no such member.</summary>
    Task<MemberSummary?> FindAsync(Guid memberKey, CancellationToken cancellationToken = default);

    /// <summary>The person an account belongs to, or null when the account has no member yet.</summary>
    Task<MemberSummary?> FindByAccountAsync(Guid userKey, CancellationToken cancellationToken = default);

    /// <summary>The account a member is linked to. Null when the member has none, or does not exist.</summary>
    Task<Guid?> FindAccountKeyAsync(Guid memberKey, CancellationToken cancellationToken = default);

    /// <summary>The kurin a member belongs to. Null when the member does not exist.</summary>
    Task<Guid?> FindKurinKeyAsync(Guid memberKey, CancellationToken cancellationToken = default);

    /// <summary>Everyone in a kurin, in one read.</summary>
    Task<IReadOnlyCollection<MemberSummary>> GetByKurinAsync(
        Guid kurinKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The kurin's members as pickable entries, with the account role each carries. Used where the
    /// caller has to choose a person — assigning a mentor, addressing a notification.
    /// </summary>
    Task<IReadOnlyCollection<MemberLookupDto>> GetLookupByKurinAsync(
        Guid kurinKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every member in the instance. Exists for the migration preflight report, which has to reason
    /// about the whole table; nothing else should need it.
    /// </summary>
    Task<IReadOnlyCollection<MemberSummary>> GetAllAsync(CancellationToken cancellationToken = default);


    /// <summary>Removes one person and answers whether there was one to remove.</summary>
    Task<bool> RemoveAsync(Guid memberKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes sure an activated account has a member, and answers with its key: links the existing
    /// record when one already carries that address, otherwise opens a new one from what the account
    /// knows. The write is left on the caller's unit of work, so it commits with the rest of the
    /// activation rather than half of it.
    /// </summary>
    Task<Guid> EnsureForAccountAsync(
        MemberForAccount details,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Carries an address change from the account to the member linked to it, and commits it. Does
    /// nothing when the account has no member. The account is the source of truth for contact details.
    /// </summary>
    Task SetEmailFromAccountAsync(Guid userKey, string email, CancellationToken cancellationToken = default);

    /// <summary>The same for the phone number.</summary>
    Task SetPhoneFromAccountAsync(Guid userKey, string phoneNumber, CancellationToken cancellationToken = default);
}
