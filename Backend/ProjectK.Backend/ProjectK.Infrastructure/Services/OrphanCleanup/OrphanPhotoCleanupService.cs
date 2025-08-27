using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Services.OrphanCleanup
{
    // Фоновий сервіс очищення сирітських blob-файлів (фото).
    // Виконує періодичне порівняння списку blob-ів із множиною референсів у БД.
    public sealed class OrphanPhotoCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrphanPhotoCleanupService> _logger;
        private readonly BlobStorageOptions _blobOptions;
        private readonly OrphanCleanupOptions _options;
        private readonly SemaphoreSlim _runLock = new(1, 1);
        private readonly Random _rnd = new();

        public OrphanPhotoCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<OrphanPhotoCleanupService> logger,
            BlobStorageOptions blobOptions,
            IOptions<OrphanCleanupOptions> cleanupOptions)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _blobOptions = blobOptions;
            _options = cleanupOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("OrphanPhotoCleanupService вимкнено (Enabled=false).");
                return;
            }

            _logger.LogInformation("OrphanPhotoCleanupService стартував. Інтервал: {Interval}, Grace: {Grace}, MaxDeletes: {Max}",
                _options.Interval, _options.GracePeriod, _options.MaxDeletesPerRun);

            // Початковий невеликий джиттер щоб не стартувати масово після деплоя
            await Task.Delay(TimeSpan.FromSeconds(_rnd.Next(0, _options.JitterSeconds + 1)), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // ігноруємо – нормальне завершення
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Помилка під час виконання очищення сирітських фото.");
                }

                var jitter = TimeSpan.FromSeconds(_rnd.Next(0, _options.JitterSeconds + 1));
                var delay = _options.Interval + jitter;

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("OrphanPhotoCleanupService зупинено.");
        }

        private async Task RunOnceAsync(CancellationToken ct)
        {
            if (!await _runLock.WaitAsync(0, ct))
            {
                _logger.LogWarning("Попередній цикл очищення ще виконується – пропускаємо цей запуск.");
                return;
            }

            try
            {
                var start = DateTime.UtcNow;
                _logger.LogInformation("Початок перевірки сирітських фото...");

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Отримати референсні blobName з БД
                var referencedBlobNames = await db.Members
                    .Where(m => m.ProfilePhotoBlobName != null && m.ProfilePhotoBlobName != "")
                    .Select(m => m.ProfilePhotoBlobName!)
                    .Distinct()
                    .ToListAsync(ct);

                var referencedSet = new HashSet<string>(referencedBlobNames, StringComparer.Ordinal);

                // 2. Підготувати Blob клієнти
                var blobService = new BlobServiceClient(_blobOptions.ConnectionString);
                var container = blobService.GetBlobContainerClient(_blobOptions.ContainerName);

                // Якщо контейнера немає – немає що чистити
                if (!await container.ExistsAsync(ct))
                {
                    _logger.LogInformation("Контейнер {Container} не існує – очищення пропущено.", _blobOptions.ContainerName);
                    return;
                }

                // 3. Зібрати всі блоби (можна з префіксом)
                var allBlobs = new List<BlobItem>();
                await foreach (var pageBlob in container.GetBlobsAsync(
                                    traits: BlobTraits.None,
                                    states: BlobStates.None,
                                    prefix: _blobOptions.BlobPrefix,
                                    cancellationToken: ct))
                {
                    allBlobs.Add(pageBlob);
                }

                _logger.LogInformation("Загальна кількість blob у контейнері {Count}, з них референсних (у БД) {Referenced}.",
                    allBlobs.Count, referencedSet.Count);

                // 4. Визначити сиріт (не у множині referenced + старші за GracePeriod)
                var now = DateTimeOffset.UtcNow;
                var graceThreshold = now - _options.GracePeriod;

                var orphans = allBlobs
                    .Where(b => !referencedSet.Contains(b.Name))
                    .Where(b =>
                    {
                        // Перевірка віку (LastModified може бути null – тоді не видаляємо для безпеки)
                        if (b.Properties.LastModified is { } lm)
                            return lm < graceThreshold;
                        return false;
                    })
                    .Select(b => b.Name)
                    .Take(_options.MaxDeletesPerRun)
                    .ToList();

                if (orphans.Count == 0)
                {
                    _logger.LogInformation("Сирітських blob для видалення не знайдено.");
                    return;
                }

                _logger.LogInformation("Знайдено {Count} сирітських blob для видалення (обмеження на прохід {Limit}). DryRun={DryRun}",
                    orphans.Count, _options.MaxDeletesPerRun, _options.DryRun);

                if (_options.DryRun)
                {
                    foreach (var name in orphans)
                        _logger.LogInformation("DryRun: orphan (не видаляємо) {BlobName}", name);
                    return;
                }

                int deleted = 0;
                foreach (var name in orphans)
                {
                    try
                    {
                        var client = container.GetBlobClient(name);
                        var resp = await client.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
                        if (resp.Value) deleted++;
                        else _logger.LogDebug("Blob {Blob} вже відсутній (можливо паралельне видалення).", name);
                    }
                    catch (RequestFailedException ex)
                    {
                        _logger.LogWarning(ex, "Помилка видалення blob {Blob}", name);
                    }
                }

                var elapsed = DateTime.UtcNow - start;
                _logger.LogInformation("Очищення завершено. Видалено {Deleted} з {Candidates} кандидатів. Тривалість {Elapsed} сек.",
                    deleted, orphans.Count, elapsed.TotalSeconds);
            }
            finally
            {
                _runLock.Release();
            }
        }
    }
}
