using ProjectK.Common.Interfaces;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Services;

/// <summary>Name lookups used to label agenda assignments (group names, member full names).</summary>
public sealed record AgendaLookups(
    IReadOnlyDictionary<Guid, string> GroupNames,
    IReadOnlyDictionary<Guid, string> MemberNames)
{
    public const string KurinLabel = "Весь курінь";

    public static async Task<AgendaLookups> LoadAsync(IUnitOfWork uow, Guid kurinKey, CancellationToken cancellationToken)
    {
        var groups = await uow.Groups.GetAllAsync(kurinKey, cancellationToken);
        var members = await uow.Members.GetAllByKurinKeyAsync(kurinKey, cancellationToken);

        var groupNames = groups.ToDictionary(g => g.GroupKey, g => g.Name);
        var memberNames = members.ToDictionary(m => m.MemberKey, m => $"{m.FirstName} {m.LastName}".Trim());

        return new AgendaLookups(groupNames, memberNames);
    }
}
