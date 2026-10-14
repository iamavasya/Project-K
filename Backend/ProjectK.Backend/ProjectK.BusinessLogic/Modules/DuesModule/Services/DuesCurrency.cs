using ProjectK.Common.Interfaces.Modules.KraiModule;

namespace ProjectK.BusinessLogic.Modules.DuesModule.Services;

/// <summary>What a kurin's вкладка is counted in: its край's currency.</summary>
public static class DuesCurrency
{
    /// <summary>Lilejka counted in гривні before краї had profiles.</summary>
    public const string Default = "UAH";

    public static async Task<string> CurrencyForKurinAsync(this IKraiProfileProvider profiles, Guid kurinKey, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetForKurinAsync(kurinKey, cancellationToken);
        return profile?.Locale.Currency ?? Default;
    }
}
