using ProjectK.Common.Entities.DuesModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.DuesModule.Services;

/// <summary>The статті a kurin had before it could name its own: one for the станиця and one for itself.</summary>
public static class KurinDuesArticles
{
    public const string StanytsiaName = "Станиця";
    public const string KurinName = "Курінь";

    public const int MaxCount = 20;
    public const int MaxNameLength = 60;

    public static IReadOnlyList<KurinDuesArticle> Standard(decimal stanytsiaFull, decimal stanytsiaReduced, decimal kurinShare) =>
    [
        new() { Name = StanytsiaName, Recipient = DuesRecipient.Stanytsia, Amount = stanytsiaFull, ReducedAmount = stanytsiaReduced },
        new() { Name = KurinName, Recipient = DuesRecipient.Kurin, Amount = kurinShare, ReducedAmount = kurinShare }
    ];

    /// <summary>
    /// Whether the статті are no more than the two standard ones, so a client that only knows the
    /// three amounts can change them without losing anything.
    /// </summary>
    public static bool AreStandard(IReadOnlyList<KurinDuesArticle> articles) =>
        articles.Count == 0
        || (articles.Count == 2
            && articles[0] is { Name: StanytsiaName, Recipient: DuesRecipient.Stanytsia }
            && articles[1] is { Name: KurinName, Recipient: DuesRecipient.Kurin }
            && articles[1].ReducedAmount == articles[1].Amount);
}
