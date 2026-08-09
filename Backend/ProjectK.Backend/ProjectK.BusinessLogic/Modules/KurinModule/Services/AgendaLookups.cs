using ProjectK.Common.Interfaces;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Services;

/// <summary>Name lookups used to label agenda assignments and creators.</summary>
public sealed record AgendaLookups(
    IReadOnlyDictionary<Guid, string> GroupNames,
    IReadOnlyDictionary<Guid, string> MemberNames,
    IReadOnlyDictionary<Guid, string> CreatorNames)
{
    public const string KurinLabel = "Весь курінь";

    public static async Task<AgendaLookups> LoadAsync(IUnitOfWork uow, Guid kurinKey, CancellationToken cancellationToken)
    {
        var groups = await uow.Groups.GetAllAsync(kurinKey, cancellationToken);
        var members = (await uow.Members.GetAllByKurinKeyAsync(kurinKey, cancellationToken)).ToList();

        var groupNames = groups.ToDictionary(g => g.GroupKey, g => g.Name);
        var memberNames = members.ToDictionary(m => m.MemberKey, m => $"{m.FirstName} {m.LastName}".Trim());

        // Creators are app users; resolve their display name through the linked member record.
        var creatorNames = members
            .Where(m => m.UserKey.HasValue && m.UserKey.Value != Guid.Empty)
            .GroupBy(m => m.UserKey!.Value)
            .ToDictionary(g => g.Key, g => $"{g.First().FirstName} {g.First().LastName}".Trim());

        return new AgendaLookups(groupNames, memberNames, creatorNames);
    }
}
