using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Authorization;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Services;

/// <inheritdoc />
public sealed class AccessContextResolver : IAccessContextResolver
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IOfficeDirectory _offices;

    public AccessContextResolver(UserManager<AppUser> userManager, IOfficeDirectory offices)
    {
        _userManager = userManager;
        _offices = offices;
    }

    public async Task<AccessContext> ResolveAsync(Guid userKey, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userKey.ToString());
        return user is null
            ? AccessContext.None(userKey)
            : await ResolveAsync(user, cancellationToken);
    }

    /// <summary>
    /// The same for a user already in hand — the sign-in paths have just loaded one, and looking it
    /// up again by key would be a second round trip for an answer they are holding.
    /// </summary>
    public async Task<AccessContext> ResolveAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { SystemRole.Member };

        // The identity store is down to one role that means anything: Admin is granted to a person,
        // not to a person-in-a-kurin, and is the only thing that outlives the kurin they are in.
        if (await _userManager.IsInRoleAsync(user, SystemRole.Admin))
        {
            roles.Add(SystemRole.Admin);
        }

        var kurinKey = user.ResolveScopeKurinKey();
        if (kurinKey.HasValue)
        {
            foreach (var office in await _offices.GetForAccountInKurinAsync(user.Id, kurinKey.Value, cancellationToken))
            {
                roles.Add(SystemRole.ForOffice(office.Type, office.Role));
            }
        }

        return new AccessContext(user.Id, kurinKey, [.. roles]);
    }
}
