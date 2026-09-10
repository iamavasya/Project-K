using ProjectK.Common.Models.Enums;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Authorization;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features;

/// <summary>Who did it, as the audit trail will remember them.</summary>
internal sealed record ProgressActor(Guid? UserKey, string? Name, string Role);

internal static class ProgressActorResolver
{
    /// <summary>
    /// The actor behind the current request: their key, the name a person will read in the trail,
    /// and the office that authorised the action.
    /// <para>
    /// The name is looked up rather than taken from the token, which carries only <c>sub</c>, the
    /// address and the roles. It used to be <c>UserId.ToString()</c> — so every signature and every
    /// вмілість event recorded a guid where a name belongs, and the screens dutifully printed it.
    /// </para>
    /// </summary>
    public static async Task<ProgressActor> ResolveAsync(
        ICurrentUserContext currentUserContext,
        IMemberDirectory members,
        CancellationToken cancellationToken)
    {
        var userKey = currentUserContext.UserId;

        return new ProgressActor(
            userKey,
            await ResolveNameAsync(userKey, members, cancellationToken),
            ResolveRole(currentUserContext));
    }

    /// <summary>
    /// The person's name, or a marker saying whose record could not be named. The markers are
    /// deliberate and readable as markers — a bare guid in the same slot reads as a name that went
    /// wrong, which is exactly the confusion this used to cause.
    /// </summary>
    private static async Task<string?> ResolveNameAsync(
        Guid? actorUserKey,
        IMemberDirectory members,
        CancellationToken cancellationToken)
    {
        if (actorUserKey is null)
        {
            return null;
        }

        var actor = await members.FindByAccountAsync(actorUserKey.Value, cancellationToken);
        if (actor is null)
        {
            return $"user:{actorUserKey.Value}";
        }

        return string.IsNullOrWhiteSpace(actor.FullName)
            ? $"member:{actor.MemberKey} / user:{actorUserKey.Value}"
            : actor.FullName;
    }

    /// <summary>
    /// The office recorded in the audit trail: admin, otherwise the one that actually authorised the
    /// action — the widest <c>Member:Update</c> scope among the offices held — and the bare baseline
    /// when none does. Ties break by name so the same user is always recorded the same way; this used
    /// to take whichever office the identity store happened to return first.
    /// </summary>
    private static string ResolveRole(ICurrentUserContext currentUserContext)
    {
        if (currentUserContext.IsInRole(SystemRole.Admin))
        {
            return SystemRole.Admin;
        }

        var office = currentUserContext.Roles
            .Where(role => !string.Equals(role, SystemRole.Member, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(role => RolePermissionMap.WidestScope(
                RolePermissionMap.Resolve(new[] { role }),
                ResourceType.Member,
                ResourceAction.Update) ?? default)
            .ThenBy(role => role, StringComparer.Ordinal)
            .FirstOrDefault();

        return office ?? SystemRole.Member;
    }
}
