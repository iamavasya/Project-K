using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Services;

/// <summary>Name lookups used to label agenda assignments and creators.</summary>
public sealed record AgendaLookups(
    IReadOnlyDictionary<Guid, string> GroupNames,
    IReadOnlyDictionary<Guid, string> MemberNames,
    IReadOnlyDictionary<Guid, string> CreatorNames,
    IReadOnlyDictionary<Guid, string> LeadershipLabels,
    IReadOnlyDictionary<Guid, AgendaCategory> Categories)
{
    public const string KurinLabel = "Весь курінь";
    public const string KvLabel = "КВ";
    public const string KurinLeadershipLabel = "Курінний провід";
    public const string GroupLeadershipLabel = "Гуртковий провід";

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

        var leadershipRefs = await uow.Leaderships.GetLeadershipRefsForKurinAsync(kurinKey, cancellationToken);
        var leadershipLabels = leadershipRefs.ToDictionary(
            r => r.LeadershipKey,
            r => LabelFor(r.Type, r.GroupKey, groupNames));

        // Include archived groups so historical items still resolve their colour/icon.
        var categories = (await uow.AgendaCategories.GetForKurinAsync(kurinKey, includeArchived: true, cancellationToken))
            .ToDictionary(c => c.AgendaCategoryKey);

        return new AgendaLookups(groupNames, memberNames, creatorNames, leadershipLabels, categories);
    }

    public static string LabelFor(LeadershipType type, Guid? groupKey, IReadOnlyDictionary<Guid, string> groupNames) =>
        type switch
        {
            LeadershipType.KV => KvLabel,
            LeadershipType.Kurin => KurinLeadershipLabel,
            LeadershipType.Group when groupKey.HasValue && groupNames.TryGetValue(groupKey.Value, out var name)
                => $"{GroupLeadershipLabel} — {name}",
            _ => GroupLeadershipLabel
        };
}
