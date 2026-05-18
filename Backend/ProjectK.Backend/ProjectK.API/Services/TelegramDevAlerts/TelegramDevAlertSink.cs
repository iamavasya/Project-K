using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ProjectK.Common.Models.Settings;
using Serilog.Core;
using Serilog.Events;

namespace ProjectK.API.Services.TelegramDevAlerts;

public sealed class TelegramDevAlertSink : ILogEventSink, IDisposable
{
    private static readonly Regex EmailRegex = new(
        @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TokenRegex = new(
        @"(?i)(bearer\s+)[a-z0-9._~+/=-]+|(?i)(token|password|secret|apikey|api_key)=([^\s;&]+)",
        RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly TelegramDevAlertOptions _options;
    private readonly string _environmentName;
    private readonly string _version;
    private readonly string _codename;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private DateTimeOffset _lastSentAt = DateTimeOffset.MinValue;

    public TelegramDevAlertSink(
        TelegramDevAlertOptions options,
        string environmentName,
        string version,
        string codename)
    {
        _options = options;
        _environmentName = environmentName;
        _version = version;
        _codename = codename;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds))
        };
    }

    public void Emit(LogEvent logEvent)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BotToken) || string.IsNullOrWhiteSpace(_options.ChatId))
        {
            return;
        }

        _ = SendAsync(logEvent);
    }

    public void Dispose()
    {
        _sendLock.Dispose();
        _httpClient.Dispose();
    }

    private async Task SendAsync(LogEvent logEvent)
    {
        if (!await _sendLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            var elapsed = DateTimeOffset.UtcNow - _lastSentAt;
            if (elapsed < TimeSpan.FromSeconds(2))
            {
                await Task.Delay(TimeSpan.FromSeconds(2) - elapsed);
            }

            var endpoint = BuildEndpoint();
            var payload = new SendMessageRequest(
                _options.ChatId!,
                BuildMessage(logEvent),
                DisableWebPagePreview: true,
                _options.DisableNotification);
            using var response = await _httpClient.PostAsJsonAsync(endpoint, payload);
            _lastSentAt = DateTimeOffset.UtcNow;
        }
        catch
        {
            // Logging sinks must never throw back into application code.
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private string BuildEndpoint()
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://api.telegram.org"
            : _options.BaseUrl.TrimEnd('/');

        return $"{baseUrl}/bot{_options.BotToken}/sendMessage";
    }

    private string BuildMessage(LogEvent logEvent)
    {
        var eventType = TryGetScalar(logEvent, "EventType") ?? "Log";
        var action = TryGetScalar(logEvent, "Action");
        var traceId = TryGetScalar(logEvent, "TraceId")
            ?? TryGetScalar(logEvent, "RequestId")
            ?? TryGetScalar(logEvent, "CorrelationId");
        var exception = logEvent.Exception == null
            ? null
            : $"{logEvent.Exception.GetType().Name}: {logEvent.Exception.Message}";

        var builder = new StringBuilder()
            .AppendLine($"[ProjectK] {_environmentName.ToUpperInvariant()} {logEvent.Level}")
            .AppendLine($"Event: {eventType}")
            .AppendLine($"Version: {_version}")
            .AppendLine($"Codename: {_codename}");

        if (!string.IsNullOrWhiteSpace(action))
        {
            builder.AppendLine($"Action: {action}");
        }

        if (!string.IsNullOrWhiteSpace(traceId))
        {
            builder.AppendLine($"Trace: {traceId}");
        }

        builder.AppendLine($"Message: {logEvent.RenderMessage()}");

        if (!string.IsNullOrWhiteSpace(exception))
        {
            builder.AppendLine($"Exception: {exception}");
        }

        return Truncate(Redact(builder.ToString().Trim()), Math.Clamp(_options.MaxMessageLength, 512, 3900));
    }

    private static string? TryGetScalar(LogEvent logEvent, string name)
    {
        return logEvent.Properties.TryGetValue(name, out var value) && value is ScalarValue scalar
            ? scalar.Value?.ToString()
            : null;
    }

    private static string Redact(string value)
    {
        var redacted = EmailRegex.Replace(value, "[email]");
        return TokenRegex.Replace(redacted, match =>
        {
            if (match.Groups[1].Success)
            {
                return $"{match.Groups[1].Value}[redacted]";
            }

            if (match.Groups[2].Success)
            {
                return $"{match.Groups[2].Value}=[redacted]";
            }

            return "[redacted]";
        });
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength
            ? value
            : string.Concat(value.AsSpan(0, maxLength - 20), "\n...[truncated]");
    }

    private sealed record SendMessageRequest(
        [property: JsonPropertyName("chat_id")] string ChatId,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("disable_web_page_preview")] bool DisableWebPagePreview,
        [property: JsonPropertyName("disable_notification")] bool DisableNotification);
}
