using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Settings;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements;

public sealed class TelegramPublicAnnouncementPublisher : IPublicAnnouncementPublisher
{
    private const int TelegramCaptionLimit = 1024;
    private const int MaxAttempts = 2;

    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramPublicAnnouncementPublisher> _logger;
    private readonly IPublicAnnouncementImageStore _imageStore;

    public TelegramPublicAnnouncementPublisher(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramPublicAnnouncementPublisher> logger,
        IPublicAnnouncementImageStore imageStore)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _imageStore = imageStore;
    }

    public async Task<PublicAnnouncementPublishResult> PublishAsync(
        PublicAnnouncementDraft draft,
        string renderedText,
        CancellationToken cancellationToken = default)
    {
        var channel = _options.PublicChannel;
        if (!channel.Enabled)
        {
            return PublicAnnouncementPublishResult.Failure("Telegram public channel is disabled.");
        }

        if (channel.DryRun)
        {
            _logger.LogInformation(
                "Telegram public announcement dry-run. DraftKey={DraftKey}, Title={Title}, Length={Length}",
                draft.PublicAnnouncementDraftKey,
                draft.Title,
                renderedText.Length);
            return PublicAnnouncementPublishResult.Success($"dry-run:{draft.PublicAnnouncementDraftKey}");
        }

        if (string.IsNullOrWhiteSpace(channel.BotToken) || string.IsNullOrWhiteSpace(channel.ChatId))
        {
            _logger.LogError("Telegram public channel is enabled but BotToken or ChatId is missing.");
            return PublicAnnouncementPublishResult.Failure("Telegram public channel is not configured.");
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, channel.TimeoutSeconds)));

            var messageIds = new List<string>();
            var endpointPrefix = BuildEndpointPrefix(channel);

            if (!string.IsNullOrWhiteSpace(draft.ImageBlobKey))
            {
                var image = await _imageStore.OpenAsync(draft.ImageBlobKey, timeoutCts.Token);
                if (image == null)
                {
                    return PublicAnnouncementPublishResult.Failure("Announcement image file was not found.");
                }

                await using var imageContent = image.Content;
                if (draft.ImagePlacement == PublicAnnouncementImagePlacement.ImageLast)
                {
                    var topCaption = renderedText.Length <= TelegramCaptionLimit ? renderedText : null;
                    if (topCaption != null)
                    {
                        var topPhotoResponse = await SendPhotoFileAsync(
                            endpointPrefix,
                            channel,
                            draft,
                            image,
                            topCaption,
                            showCaptionAboveMedia: true,
                            timeoutCts.Token);

                        if (!topPhotoResponse.Succeeded)
                        {
                            return topPhotoResponse;
                        }

                        AddMessageId(messageIds, topPhotoResponse.TelegramMessageId);
                        return PublicAnnouncementPublishResult.Success(string.Join(",", messageIds));
                    }

                    var textResponse = await SendTextAsync(endpointPrefix, channel, draft, renderedText, timeoutCts.Token);
                    if (!textResponse.Succeeded)
                    {
                        return textResponse;
                    }

                    AddMessageId(messageIds, textResponse.TelegramMessageId);

                    var bottomPhotoResponse = await SendPhotoFileAsync(
                        endpointPrefix,
                        channel,
                        draft,
                        image,
                        caption: null,
                        showCaptionAboveMedia: false,
                        timeoutCts.Token);

                    if (!bottomPhotoResponse.Succeeded)
                    {
                        return PublicAnnouncementPublishResult.Failure(
                            bottomPhotoResponse.ErrorMessage ?? "Telegram photo message failed after text was sent.",
                            string.Join(",", messageIds),
                            partiallySucceeded: true);
                    }

                    AddMessageId(messageIds, bottomPhotoResponse.TelegramMessageId);
                    return PublicAnnouncementPublishResult.Success(string.Join(",", messageIds));
                }

                var caption = renderedText.Length <= TelegramCaptionLimit ? renderedText : null;
                var photoResponse = await SendPhotoFileAsync(
                    endpointPrefix,
                    channel,
                    draft,
                    image,
                    caption,
                    showCaptionAboveMedia: false,
                    timeoutCts.Token);
                if (!photoResponse.Succeeded)
                {
                    return photoResponse;
                }

                AddMessageId(messageIds, photoResponse.TelegramMessageId);

                if (caption == null)
                {
                    var textResponse = await SendTextAsync(endpointPrefix, channel, draft, renderedText, timeoutCts.Token);
                    if (!textResponse.Succeeded)
                    {
                        return PublicAnnouncementPublishResult.Failure(
                            textResponse.ErrorMessage ?? "Telegram text message failed after photo was sent.",
                            string.Join(",", messageIds),
                            partiallySucceeded: true);
                    }

                    AddMessageId(messageIds, textResponse.TelegramMessageId);
                }

                return PublicAnnouncementPublishResult.Success(string.Join(",", messageIds));
            }

            if (!string.IsNullOrWhiteSpace(draft.ImageUrl))
            {
                if (draft.ImagePlacement == PublicAnnouncementImagePlacement.ImageLast)
                {
                    var topCaption = renderedText.Length <= TelegramCaptionLimit ? renderedText : null;
                    if (topCaption != null)
                    {
                        var topPhotoResponse = await PostAsync(
                            $"{endpointPrefix}/sendPhoto",
                            new SendPhotoUrlRequest(
                                channel.ChatId,
                                draft.ImageUrl,
                                topCaption,
                                ToTelegramParseMode(draft.ParseMode),
                                channel.DisableNotification,
                                ShowCaptionAboveMedia: true),
                            timeoutCts.Token);

                        if (!topPhotoResponse.Succeeded)
                        {
                            return topPhotoResponse;
                        }

                        AddMessageId(messageIds, topPhotoResponse.TelegramMessageId);
                        return PublicAnnouncementPublishResult.Success(string.Join(",", messageIds));
                    }

                    var textResponse = await SendTextAsync(endpointPrefix, channel, draft, renderedText, timeoutCts.Token);
                    if (!textResponse.Succeeded)
                    {
                        return textResponse;
                    }

                    AddMessageId(messageIds, textResponse.TelegramMessageId);

                    var bottomPhotoResponse = await PostAsync(
                        $"{endpointPrefix}/sendPhoto",
                        new SendPhotoUrlRequest(
                            channel.ChatId,
                            draft.ImageUrl,
                            Caption: null,
                            ParseMode: null,
                            DisableNotification: channel.DisableNotification,
                            ShowCaptionAboveMedia: false),
                        timeoutCts.Token);

                    if (!bottomPhotoResponse.Succeeded)
                    {
                        return PublicAnnouncementPublishResult.Failure(
                            bottomPhotoResponse.ErrorMessage ?? "Telegram photo message failed after text was sent.",
                            string.Join(",", messageIds),
                            partiallySucceeded: true);
                    }

                    AddMessageId(messageIds, bottomPhotoResponse.TelegramMessageId);
                    return PublicAnnouncementPublishResult.Success(string.Join(",", messageIds));
                }

                var caption = renderedText.Length <= TelegramCaptionLimit ? renderedText : null;
                var photoResponse = await PostAsync(
                    $"{endpointPrefix}/sendPhoto",
                    new SendPhotoUrlRequest(
                        channel.ChatId,
                        draft.ImageUrl,
                        caption,
                        caption == null ? null : ToTelegramParseMode(draft.ParseMode),
                        channel.DisableNotification,
                        ShowCaptionAboveMedia: false),
                    timeoutCts.Token);

                if (!photoResponse.Succeeded)
                {
                    return photoResponse;
                }

                AddMessageId(messageIds, photoResponse.TelegramMessageId);

                if (caption == null)
                {
                    var textResponse = await SendTextAsync(endpointPrefix, channel, draft, renderedText, timeoutCts.Token);
                    if (!textResponse.Succeeded)
                    {
                        return PublicAnnouncementPublishResult.Failure(
                            textResponse.ErrorMessage ?? "Telegram text message failed after photo was sent.",
                            string.Join(",", messageIds),
                            partiallySucceeded: true);
                    }

                    AddMessageId(messageIds, textResponse.TelegramMessageId);
                }

                return PublicAnnouncementPublishResult.Success(string.Join(",", messageIds));
            }

            return await SendTextAsync(endpointPrefix, channel, draft, renderedText, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Telegram public announcement publish timed out. DraftKey={DraftKey}",
                draft.PublicAnnouncementDraftKey);
            return PublicAnnouncementPublishResult.Failure("Telegram publish timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Telegram public announcement publish failed. DraftKey={DraftKey}",
                draft.PublicAnnouncementDraftKey);
            return PublicAnnouncementPublishResult.Failure("Telegram publish failed.");
        }
    }

    private async Task<PublicAnnouncementPublishResult> SendPhotoFileAsync(
        string endpointPrefix,
        TelegramChannelOptions channel,
        PublicAnnouncementDraft draft,
        PublicAnnouncementImageFile image,
        string? caption,
        bool showCaptionAboveMedia,
        CancellationToken cancellationToken)
    {
        await using var imageBuffer = new MemoryStream();
        await image.Content.CopyToAsync(imageBuffer, cancellationToken);
        var imageBytes = imageBuffer.ToArray();

        return await PostMultipartAsync($"{endpointPrefix}/sendPhoto", () =>
        {
            var form = new MultipartFormDataContent
            {
                { new StringContent(channel.ChatId!), "chat_id" },
                { new StringContent(channel.DisableNotification ? "true" : "false"), "disable_notification" }
            };

            if (caption != null)
            {
                form.Add(new StringContent(caption), "caption");
                if (showCaptionAboveMedia)
                {
                    form.Add(new StringContent("true"), "show_caption_above_media");
                }

                var parseMode = ToTelegramParseMode(draft.ParseMode);
                if (parseMode != null)
                {
                    form.Add(new StringContent(parseMode), "parse_mode");
                }
            }

            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
            form.Add(fileContent, "photo", image.FileName);

            return form;
        }, cancellationToken);
    }

    private async Task<PublicAnnouncementPublishResult> SendTextAsync(
        string endpointPrefix,
        TelegramChannelOptions channel,
        PublicAnnouncementDraft draft,
        string renderedText,
        CancellationToken cancellationToken)
    {
        return await PostAsync(
            $"{endpointPrefix}/sendMessage",
            new SendMessageRequest(
                channel.ChatId!,
                renderedText,
                ToTelegramParseMode(draft.ParseMode),
                channel.DisableNotification),
            cancellationToken);
    }

    private async Task<PublicAnnouncementPublishResult> PostAsync<TRequest>(
        string url,
        TRequest request,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
                TelegramApiResponse? payload = null;

                try
                {
                    payload = await response.Content.ReadFromJsonAsync<TelegramApiResponse>(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse Telegram API response.");
                }

                if (response.IsSuccessStatusCode && payload?.Ok == true)
                {
                    return PublicAnnouncementPublishResult.Success(payload.Result?.MessageId.ToString());
                }

                var description = payload?.Description ?? response.ReasonPhrase ?? "Unknown Telegram API error.";
                _logger.LogWarning(
                    "Telegram API request failed. StatusCode={StatusCode}, Description={Description}, Attempt={Attempt}",
                    (int)response.StatusCode,
                    description,
                    attempt);

                if (attempt < MaxAttempts && IsTransient(response.StatusCode))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                    continue;
                }

                return PublicAnnouncementPublishResult.Failure(description);
            }
            catch (HttpRequestException ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(ex, "Telegram API request failed with transient HTTP error. Attempt={Attempt}", attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }
        }

        return PublicAnnouncementPublishResult.Failure("Telegram API request failed.");
    }

    private async Task<PublicAnnouncementPublishResult> PostMultipartAsync(
        string url,
        Func<MultipartFormDataContent> createForm,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var form = createForm();
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = form
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            TelegramApiResponse? payload = null;

            try
            {
                payload = await response.Content.ReadFromJsonAsync<TelegramApiResponse>(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Telegram API response.");
            }

            if (response.IsSuccessStatusCode && payload?.Ok == true)
            {
                return PublicAnnouncementPublishResult.Success(payload.Result?.MessageId.ToString());
            }

            var description = payload?.Description ?? response.ReasonPhrase ?? "Unknown Telegram API error.";
            _logger.LogWarning(
                "Telegram multipart API request failed. StatusCode={StatusCode}, Description={Description}, Attempt={Attempt}",
                (int)response.StatusCode,
                description,
                attempt);

            if (attempt < MaxAttempts && IsTransient(response.StatusCode))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                continue;
            }

            return PublicAnnouncementPublishResult.Failure(description);
        }

        return PublicAnnouncementPublishResult.Failure("Telegram API request failed.");
    }

    private static string BuildEndpointPrefix(TelegramChannelOptions channel)
    {
        var baseUrl = string.IsNullOrWhiteSpace(channel.BaseUrl)
            ? "https://api.telegram.org"
            : channel.BaseUrl.TrimEnd('/');

        return $"{baseUrl}/bot{channel.BotToken}";
    }

    private static string? ToTelegramParseMode(PublicAnnouncementParseMode parseMode)
    {
        return parseMode switch
        {
            PublicAnnouncementParseMode.Html => "HTML",
            PublicAnnouncementParseMode.MarkdownV2 => "MarkdownV2",
            _ => null
        };
    }

    private static bool IsTransient(System.Net.HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return statusCode is System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.TooManyRequests
            || code >= 500;
    }

    private static void AddMessageId(ICollection<string> messageIds, string? messageId)
    {
        if (!string.IsNullOrWhiteSpace(messageId))
        {
            messageIds.Add(messageId);
        }
    }

    private sealed record SendMessageRequest(
        [property: JsonPropertyName("chat_id")] string ChatId,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("parse_mode")] string? ParseMode,
        [property: JsonPropertyName("disable_notification")] bool DisableNotification);

    private sealed record SendPhotoUrlRequest(
        [property: JsonPropertyName("chat_id")] string ChatId,
        [property: JsonPropertyName("photo")] string Photo,
        [property: JsonPropertyName("caption")] string? Caption,
        [property: JsonPropertyName("parse_mode")] string? ParseMode,
        [property: JsonPropertyName("disable_notification")] bool DisableNotification,
        [property: JsonPropertyName("show_caption_above_media")] bool ShowCaptionAboveMedia);

    private sealed record TelegramApiResponse(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("result")] TelegramMessageResult? Result,
        [property: JsonPropertyName("description")] string? Description);

    private sealed record TelegramMessageResult(
        [property: JsonPropertyName("message_id")] int MessageId);
}
