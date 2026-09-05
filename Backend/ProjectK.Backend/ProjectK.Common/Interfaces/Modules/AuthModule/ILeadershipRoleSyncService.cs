namespace ProjectK.Common.Interfaces.Modules.AuthModule;

/// <summary>
/// Keeps a member's office roles equal to the offices they currently hold. The діловодство registry
/// (<c>LeadershipHistory</c>) is the source of truth; this realigns the identity roles whenever an
/// office is assigned or ended. It never touches the Admin role or any role it does not manage.
/// <para>
/// The contract lives in Common because the seeders in Infrastructure depend on it while the
/// implementation stays in the business layer.
/// </para>
/// </summary>
public interface ILeadershipRoleSyncService
{
    /// <summary>Realigns one member's office roles with their active offices.</summary>
    Task SyncMemberAsync(Guid memberKey, CancellationToken cancellationToken = default);

    /// <summary>Realigns several members (e.g. everyone touched by an <c>UpsertLeadership</c> call).</summary>
    Task SyncMembersAsync(IEnumerable<Guid> memberKeys, CancellationToken cancellationToken = default);
}
