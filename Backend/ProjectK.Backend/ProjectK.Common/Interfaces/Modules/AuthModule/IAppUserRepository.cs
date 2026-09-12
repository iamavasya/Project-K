using ProjectK.Common.Entities.AuthModule;

namespace ProjectK.Common.Interfaces.Modules.AuthModule;

/// <summary>
/// The account queries the business layer needs.
/// <para>
/// These used to be written as LINQ over <c>UserManager.Users</c>, which is an <c>IQueryable</c> —
/// so the handlers had to reference EF Core to await it, and the shape of the query lived in the
/// handler rather than behind the repository line like every other read.
/// </para>
/// </summary>
public interface IAppUserRepository
{
    Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The account holding an address, or <c>null</c>. Used where an onboarding record has to be
    /// tied back to the account it belongs to and only the address is at hand.
    /// </summary>
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// How many of these accounts finished onboarding. The caller names the accounts because who
    /// belongs to a kurin is a question for membership, and an account record cannot answer it:
    /// <c>AppUser.KurinKey</c> is written once when the account is opened and never again.
    /// </summary>
    Task<int> CountActiveAsync(
        IReadOnlyCollection<Guid> userKeys,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Forgets the kurin on every account that names it, whether as the one stepped into
    /// (<c>ActiveKurinKey</c>) or as the one written at sign-up (<c>KurinKey</c>). Neither column is a
    /// foreign key, so deleting a kurin used to leave these accounts scoped to a key that no longer
    /// existed: signed in, shown a kurin, and with nowhere to step out to. The change is tracked and
    /// lands with the caller's <c>SaveChangesAsync</c>.
    /// </summary>
    Task DetachFromKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);
}
