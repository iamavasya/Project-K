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

    /// <summary>Accounts that finished onboarding in the kurin.</summary>
    Task<int> CountActiveAsync(Guid kurinKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active beta participants, across the system when <paramref name="kurinKey"/> is <c>null</c>
    /// and inside one kurin otherwise. This is what the beta cap is measured against.
    /// </summary>
    Task<int> CountActiveBetaAsync(Guid? kurinKey, CancellationToken cancellationToken = default);
}
