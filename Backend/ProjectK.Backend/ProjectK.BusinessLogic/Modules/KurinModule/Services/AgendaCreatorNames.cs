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

        var missing = items
            .Select(item => item.CreatedByUserKey)
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
