using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectK.Infrastructure.Services.WebPush;

namespace ProjectK.Infrastructure.BackgroundServices;

/// <summary>
/// Sends the pushes the requests queued (<see cref="ChannelPushQueue"/>), one notification at a time,
/// each in its own DI scope like a request of its own. Registered only when VAPID keys are
/// configured. A failure is logged and the next notification goes on; nothing here can reach back
/// into the request that raised it.
/// </summary>
public sealed class WebPushBackgroundService : BackgroundService
{
    private readonly ChannelPushQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebPushBackgroundService> _logger;

    public WebPushBackgroundService(
        ChannelPushQueue queue,
        IServiceProvider serviceProvider,
        ILogger<WebPushBackgroundService> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Web push is on.");

        try
        {
            await foreach (var notification in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<WebPushDispatcher>();
                    await dispatcher.DeliverAsync(notification, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Web push for notification {NotificationKey} failed.", notification.NotificationKey);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The host is stopping; what is still queued is dropped with it.
        }
    }
}
