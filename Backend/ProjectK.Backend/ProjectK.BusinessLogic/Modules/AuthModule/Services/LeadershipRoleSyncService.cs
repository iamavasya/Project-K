using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Services;

public sealed class LeadershipRoleSyncService : ILeadershipRoleSyncService
{
    // Roles this service owns: Member plus every office role. Admin and anything else are left alone.
    private static readonly IReadOnlyCollection<string> ManagedRoles =
        SystemRole.All().Where(role => role != SystemRole.Admin).ToArray();

    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<AppUser> _userManager;

    public LeadershipRoleSyncService(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task SyncMembersAsync(IEnumerable<Guid> memberKeys, CancellationToken cancellationToken = default)
    {
        foreach (var memberKey in memberKeys.Distinct())
        {
            await SyncMemberAsync(memberKey, cancellationToken);
        }
    }

    public async Task SyncMemberAsync(Guid memberKey, CancellationToken cancellationToken = default)
    {
        var userKey = await _unitOfWork.Members.GetUserKeyByMemberAsync(memberKey, cancellationToken);
        if (userKey is null)
        {
            // No linked account yet — nothing to grant. Sync runs again when the account is linked.
            return;
        }

        var user = await _userManager.FindByIdAsync(userKey.Value.ToString());
        if (user is null)
        {
            return;
        }

        var offices = await _unitOfWork.Leaderships.GetActiveOfficesForMemberAsync(memberKey, cancellationToken);

        var target = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { SystemRole.Member };
        foreach (var office in offices)
        {
            target.Add(SystemRole.ForOffice(office.Type, office.Role));
        }

        // A mentor assignment makes the member a Впорядник (КВ) of the group — not the youth Гуртковий.
        // It is folded into the role set so one sync path stays the single source for a member's roles.
        var mentorAssignments = await _unitOfWork.MentorAssignments.GetByMentorUserKeyAsync(userKey.Value, cancellationToken);
        if (mentorAssignments.Any(assignment => assignment.RevokedAtUtc is null))
        {
            target.Add(SystemRole.ForOffice(LeadershipType.KV, LeadershipRole.Vykhovnyk));
        }

        var current = await _userManager.GetRolesAsync(user);
        var managedCurrent = current
            .Where(role => ManagedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = target.Where(role => !managedCurrent.Contains(role)).ToArray();
        var toRemove = managedCurrent.Where(role => !target.Contains(role)).ToArray();

        if (toRemove.Length > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, toRemove);
        }

        if (toAdd.Length > 0)
        {
            await _userManager.AddToRolesAsync(user, toAdd);
        }
    }
}
