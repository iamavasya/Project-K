using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectK.Common.Models.Settings;

namespace ProjectK.Infrastructure.Services.Catalog;

/// <summary>
/// Keeps <see cref="CatalogStore"/> current. At startup it loads the copy on disk and, when there is
/// none, waits for the first fetch so the API does not open with an empty catalogue. After that it
/// asks the service again every <see cref="CatalogOptions.RefreshMinutes"/>; an unchanged catalogue
/// costs a 304. A failed fetch keeps what is already loaded.
/// </summary>
internal sealed class CatalogSyncBackgroundService : BackgroundService
{
    private readonly CatalogStore _store;
    private readonly CatalogClient _client;
    private readonly CatalogOptions _options;
    private readonly ILogger<CatalogSyncBackgroundService> _logger;
    private readonly string _snapshotPath;
    private string? _etag;
    private bool _fetchedAtStart;

    public CatalogSyncBackgroundService(
        CatalogStore store,
        CatalogClient client,
        IOptions<CatalogOptions> options,
        ILogger<CatalogSyncBackgroundService> logger)
    {
        _store = store;
        _client = client;
        _options = options.Value;
        _logger = logger;
        _snapshotPath = Path.IsPathRooted(_options.SnapshotPath)
            ? _options.SnapshotPath
            : Path.Combine(AppContext.BaseDirectory, _options.SnapshotPath);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        LoadSnapshot();
        if (!_store.IsLoaded)
        {
            _fetchedAtStart = await RefreshAsync(cancellationToken);
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_fetchedAtStart)
        {
            await RefreshAsync(stoppingToken);
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.RefreshMinutes));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await RefreshAsync(stoppingToken);
        }
    }

    /// <summary>One fetch. True when the store holds the service's current catalogue afterwards.</summary>
    internal async Task<bool> RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var fetched = await _client.FetchAsync(_store.IsLoaded ? _etag : null, cancellationToken);
            if (fetched is null)
            {
                _logger.LogDebug("Catalogue unchanged (revision {Revision}).", _store.State.Revision);
                return true;
            }

            var (body, etag) = fetched.Value;
            var state = CatalogMapper.ToState(CatalogClient.Parse(body), _options.BaseUrl, DateTimeOffset.UtcNow);
            if (state.Badges.Count == 0 && state.Probes.Count == 0 && _store.IsLoaded)
            {
                _logger.LogWarning("The catalogue answered with no вмілості and no проби; keeping revision {Revision}.", _store.State.Revision);
                return false;
            }

            _store.Replace(state);
            _etag = etag;
            SaveSnapshot(body, etag);
            _logger.LogInformation(
                "Catalogue revision {Revision} loaded from {Url}: {Badges} вмілостей, {Probes} проб.",
                state.Revision, _client.CatalogUrl, state.Badges.Count, state.Probes.Count);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not fetch the catalogue from {Url}; keeping revision {Revision} ({Badges} вмілостей).",
                _client.CatalogUrl, _store.State.Revision, _store.State.Badges.Count);
            return false;
        }
    }

    private void LoadSnapshot()
    {
        if (!File.Exists(_snapshotPath))
        {
            return;
        }

        try
        {
            var body = File.ReadAllBytes(_snapshotPath);
            var fetchedAt = new DateTimeOffset(File.GetLastWriteTimeUtc(_snapshotPath), TimeSpan.Zero);
            _store.Replace(CatalogMapper.ToState(CatalogClient.Parse(body), _options.BaseUrl, fetchedAt));
            _etag = File.Exists(EtagPath) ? File.ReadAllText(EtagPath).Trim() : null;
            _logger.LogInformation("Catalogue revision {Revision} loaded from {Path}.", _store.State.Revision, _snapshotPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ignoring the unreadable catalogue copy at {Path}.", _snapshotPath);
        }
    }

    private void SaveSnapshot(byte[] body, string? etag)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_snapshotPath)!);
            var temp = _snapshotPath + ".tmp";
            File.WriteAllBytes(temp, body);
            File.Move(temp, _snapshotPath, overwrite: true);
            if (etag is null)
            {
                File.Delete(EtagPath);
            }
            else
            {
                File.WriteAllText(EtagPath, etag);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save the catalogue copy to {Path}; the next restart fetches it again.", _snapshotPath);
        }
    }

    private string EtagPath => _snapshotPath + ".etag";
}
