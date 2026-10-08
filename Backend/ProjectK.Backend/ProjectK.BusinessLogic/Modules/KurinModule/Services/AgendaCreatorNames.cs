using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule.Agenda;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Services;

/// <summary>
/// Resolves agenda creator display names. Most creators are members (resolved cheaply from the member
/// list); creators without a member record in this kurin — e.g. an admin — are looked up through the
/// app user so authorship still shows.
/// </summary>
public static class AgendaCreatorNames
{
    public static async Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        UserManager<AppUser> userManager,
        IReadOnlyDictionary<Guid, string> fromMembers,
        IEnumerable<AgendaItem> items,
        CancellationToken cancellationToken)
    {
        var names = new Dictionary<Guid, string>(fromMembers);

        // Authors, and whoever last moved a target or a part — the board names them all.
        var missing = items
            .SelectMany(item => item.Assignments
                .SelectMany(a => a.Progress.Select(p => (Guid?)p.ChangedByUserKey).Append(a.StatusChangedByUserKey))
                .Where(key => key.HasValue)
                .Select(key => key!.Value)
                .Append(item.CreatedByUserKey)
                .Concat(item.ArchivedByUserKey is { } archivedBy ? [archivedBy] : []))
            .Where(key => key != Guid.Empty && !names.ContainsKey(key))
            .Distinct();

        foreach (var key in missing)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await userManager.FindByIdAsync(key.ToString());
            if (user is null)
            {
                continue;
            }

            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            names[key] = string.IsNullOrWhiteSpace(fullName) ? (user.Email ?? "—") : fullName;
        }

        return names;
    }
}
