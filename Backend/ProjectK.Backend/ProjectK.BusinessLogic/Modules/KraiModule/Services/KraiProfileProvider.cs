using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.BusinessLogic.Services.Caching;
using ProjectK.Common.Entities.KraiModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KraiModule;
using ProjectK.Common.Models.KraiProfiles;

namespace ProjectK.BusinessLogic.Modules.KraiModule.Services;

/// <summary>
/// Builds a край's profile: the values in code first, the край's latest saved revision over them.
/// The values in code are how Lilejka worked before profiles, so an empty profile changes nothing.
/// </summary>
public sealed class KraiProfileProvider : IKraiProfileProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackendCache _cache;
    private readonly ILogger<KraiProfileProvider> _logger;

    public KraiProfileProvider(IUnitOfWork unitOfWork, IBackendCache cache, ILogger<KraiProfileProvider> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public Task<KraiProfile?> GetForKraiAsync(string kraiCode, CancellationToken cancellationToken = default)
    {
        return _cache.GetOrCreateAsync(
            BackendCachePolicies.KraiProfileReads,
            kraiCode,
            token => BuildAsync(kraiCode, token),
            cancellationToken);
    }

    public async Task<KraiProfile?> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default)
    {
        var kurin = await _unitOfWork.Kurins.GetByKeyAsync(kurinKey, cancellationToken);
        return kurin is null ? null : await GetForKraiAsync(kurin.KraiCode, cancellationToken);
    }

    private async Task<KraiProfile?> BuildAsync(string kraiCode, CancellationToken cancellationToken)
    {
        var krai = await _unitOfWork.Krais.GetAsync(kraiCode, cancellationToken);
        if (krai is null)
        {
            return null;
        }

        var revision = await _unitOfWork.Krais.GetLatestProfileRevisionAsync(kraiCode, cancellationToken);
        return Resolve(krai, revision, Read(revision));
    }

    private KraiProfileDocument Read(KraiProfileRevision? revision)
    {
        if (revision is null || string.IsNullOrWhiteSpace(revision.Json))
        {
            return new KraiProfileDocument();
        }

        // A revision newer than this Lilejka understands is still read: fields it does not know
        // are skipped, the ones it knows keep their meaning. A schema change that renames or
        // reinterprets a field adds its conversion here, keyed on SchemaVersion.
        try
        {
            return JsonSerializer.Deserialize<KraiProfileDocument>(revision.Json, JsonOptions) ?? new KraiProfileDocument();
        }
        catch (JsonException exception)
        {
            // A broken profile must not take the kurins of its край down with it; they work on
            // the values in code until an admin saves a good one.
            _logger.LogError(exception, "Profile revision {Revision} of край {Krai} is not valid JSON; using the defaults.", revision.Revision, revision.KraiCode);
            return new KraiProfileDocument();
        }
    }

    private static KraiProfile Resolve(Krai krai, KraiProfileRevision? revision, KraiProfileDocument document)
    {
        var catalog = document.Catalog;
        var locale = document.Locale;

        return new KraiProfile(
            krai.Code,
            krai.Name,
            revision?.Revision ?? 0,
            new KraiCatalogSettings(
                BadgeSources: Codes(catalog?.BadgeSources) ?? [krai.Code],
                ProbeSources: Codes(catalog?.ProbeSources) ?? [krai.Code],
                HiddenBadges: Ids(catalog?.HiddenBadges),
                HiddenProbes: Ids(catalog?.HiddenProbes)),
            new KraiLocaleSettings(
                Currency: Text(locale?.Currency)?.ToUpperInvariant() ?? "UAH",
                TimeZone: Text(locale?.TimeZone) ?? AgendaRecurrence.DefaultZone.Id,
                NumberLocale: Text(locale?.NumberLocale) ?? "uk-UA"));
    }

    // An empty list of sources is read as "not set", never as "show nothing".
    private static IReadOnlyList<string>? Codes(IReadOnlyList<string>? codes)
    {
        var cleaned = codes?
            .Select(Text)
            .OfType<string>()
            .Select(code => code.ToLowerInvariant())
            .Distinct()
            .ToList();
        return cleaned is { Count: > 0 } ? cleaned : null;
    }

    private static IReadOnlyList<string> Ids(IReadOnlyList<string>? ids)
    {
        return ids?.Select(Text).OfType<string>().Distinct().ToList() ?? [];
    }

    private static string? Text(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
