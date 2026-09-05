using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Authorization;

/// <summary>
/// The access layer's role identities, stored as ASP.NET Identity roles. Office roles mirror the
/// діловодство registry one-to-one (<c>{провід}.{офіс}</c>, e.g. <c>KV.Zvyazkovyi</c>) and are kept
/// in sync from active <c>LeadershipHistory</c> by <c>ILeadershipRoleSyncService</c>. <see cref="Admin"/>
/// is system-level and assigned independently; <see cref="Member"/> is the baseline every
/// authenticated member carries.
/// </summary>
public static class SystemRole
{
    public const string Admin = "Admin";
    public const string Member = "Member";

    /// <summary>The system-role name for an office within its провід.</summary>
    public static string ForOffice(LeadershipType type, LeadershipRole role) => $"{type}.{role}";

    /// <summary>Every role name that should exist in the identity store.</summary>
    public static IReadOnlyList<string> All() =>
        new[] { Admin, Member }
            .Concat(LeadershipOffices.All().Select(office => ForOffice(office.Type, office.Role)))
            .ToArray();

    // Deliberately no "who is privileged" list here. That question is answered by
    // RolePermissionMap.GrantsWholeKurinManagement, derived from the grant tables — a second
    // hardcoded list drifted from it once already and left Курінний unable to disable an MFA
    // he was never required to enable.
}
