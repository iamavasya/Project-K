using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements;
using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Commands;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Handlers;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Settings;
using ProjectK.Infrastructure.DbContexts;
using ProjectK.Infrastructure.UnitOfWork;

namespace ProjectK.BusinessLogic.Tests.InfrastructureModule.PublicAnnouncements;

public class PublicAnnouncementDraftWorkflowTests
{
    [Fact]
    public async Task CreateDraft_ShouldPersistDraftWithRenderedText()
    {
        await using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var userId = Guid.NewGuid();
        var currentUserContext = CreateCurrentUserContext(userId);
        var activityLogger = new Mock<IActivityLogger>();
        var imageStore = new Mock<IPublicAnnouncementImageStore>();
        var handler = new CreatePublicAnnouncementDraftCommandHandler(
            unitOfWork,
            currentUserContext.Object,
            new PublicAnnouncementRenderer(),
            activityLogger.Object);

        var result = await handler.Handle(
            new CreatePublicAnnouncementDraftCommand(
                "Release",
                "ProjectK updated.",
                PublicAnnouncementSourceType.Manual,
                null,
                null,
                "production",
                "1.0.0",
                "Ant Era",
                PublicAnnouncementParseMode.PlainText,
                null,
                null,
                null,
                PublicAnnouncementImagePlacement.ImageFirst,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(ResultType.Created, result.Type);
        Assert.NotNull(result.Data);
        Assert.Equal("Release\n\nProjectK updated.", result.Data!.RenderedText);
        Assert.Equal(userId, result.Data.CreatedByUserKey);
        Assert.Equal(1, await context.PublicAnnouncementDrafts.CountAsync());
        activityLogger.Verify(x => x.LogAudit(
            "Announcement.Created",
            userId,
            null,
            null,
            null,
            It.Is<string>(reason => reason.Contains(result.Data.PublicAnnouncementDraftKey.ToString()))),
            Times.Once);
    }

    [Fact]
    public async Task CreateDraft_WhenSourceAlreadyHasActiveDraft_ShouldRejectDuplicate()
    {
        await using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var currentUserContext = CreateCurrentUserContext(Guid.NewGuid());
        var activityLogger = new Mock<IActivityLogger>();
        var handler = new CreatePublicAnnouncementDraftCommandHandler(
            unitOfWork,
            currentUserContext.Object,
            new PublicAnnouncementRenderer(),
            activityLogger.Object);

        var command = new CreatePublicAnnouncementDraftCommand(
            "Release",
            "ProjectK updated.",
            PublicAnnouncementSourceType.GitHubRelease,
            "release-1",
            null,
            null,
            null,
            null,
            PublicAnnouncementParseMode.PlainText,
            null,
            null,
            null,
            PublicAnnouncementImagePlacement.ImageFirst,
            null,
            null);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultType.Created, first.Type);
        Assert.Equal(ResultType.Conflict, second.Type);
        Assert.Equal("DuplicateAnnouncementSource", second.ErrorCode);
    }

    [Fact]
    public async Task CreateDraft_ShouldRejectSensitiveData()
    {
        await using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var currentUserContext = CreateCurrentUserContext(Guid.NewGuid());
        var activityLogger = new Mock<IActivityLogger>();
        var handler = new CreatePublicAnnouncementDraftCommandHandler(
            unitOfWork,
            currentUserContext.Object,
            new PublicAnnouncementRenderer(),
            activityLogger.Object);

        var result = await handler.Handle(
            new CreatePublicAnnouncementDraftCommand(
                "Release",
                "Contact admin at admin@example.com",
                PublicAnnouncementSourceType.Manual,
                null,
                null,
                "production",
                "1.0.0",
                "Ant Era",
                PublicAnnouncementParseMode.PlainText,
                null,
                null,
                null,
                PublicAnnouncementImagePlacement.ImageFirst,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(ResultType.BadRequest, result.Type);
        Assert.Equal("DraftContainsSensitiveData", result.ErrorCode);
    }

    [Fact]
    public async Task PublishDraft_ShouldRequireApprovedDraftAndMarkPublished()
    {
        await using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var userId = Guid.NewGuid();
        var currentUserContext = CreateCurrentUserContext(userId);
        var activityLogger = new Mock<IActivityLogger>();
        var imageStore = new Mock<IPublicAnnouncementImageStore>();
        var renderer = new PublicAnnouncementRenderer();
        var publisher = new NullPublicAnnouncementPublisher(NullLogger<NullPublicAnnouncementPublisher>.Instance);

        var createHandler = new CreatePublicAnnouncementDraftCommandHandler(unitOfWork, currentUserContext.Object, renderer, activityLogger.Object);
        var transitionHandler = new TransitionPublicAnnouncementDraftCommandHandler(unitOfWork, currentUserContext.Object, activityLogger.Object, imageStore.Object);
        var publishHandler = new PublishPublicAnnouncementDraftCommandHandler(unitOfWork, currentUserContext.Object, renderer, publisher, activityLogger.Object, imageStore.Object);

        var created = await createHandler.Handle(
            new CreatePublicAnnouncementDraftCommand(
                "Maintenance",
                "Short planned maintenance.",
                PublicAnnouncementSourceType.Manual,
                null,
                null,
                null,
                null,
                null,
                PublicAnnouncementParseMode.PlainText,
                null,
                null,
                null,
                PublicAnnouncementImagePlacement.ImageFirst,
                null,
                null),
            CancellationToken.None);

        var draftKey = created.Data!.PublicAnnouncementDraftKey;
        var publishBeforeApprove = await publishHandler.Handle(new PublishPublicAnnouncementDraftCommand(draftKey), CancellationToken.None);
        Assert.Equal(ResultType.BadRequest, publishBeforeApprove.Type);
        Assert.Equal("DraftNotApproved", publishBeforeApprove.ErrorCode);

        await transitionHandler.Handle(
            new TransitionPublicAnnouncementDraftCommand(draftKey, PublicAnnouncementStatus.Approved),
            CancellationToken.None);

        var published = await publishHandler.Handle(new PublishPublicAnnouncementDraftCommand(draftKey), CancellationToken.None);

        Assert.Equal(ResultType.Success, published.Type);
        Assert.Equal(PublicAnnouncementStatus.Published, published.Data!.Status);
        Assert.Equal(userId, published.Data.PublishedByUserKey);
        Assert.StartsWith("dry-run:", published.Data.TelegramMessageId);

        var publishAgain = await publishHandler.Handle(new PublishPublicAnnouncementDraftCommand(draftKey), CancellationToken.None);
        Assert.Equal(ResultType.Conflict, publishAgain.Type);
        Assert.Equal("DraftAlreadyPublished", publishAgain.ErrorCode);
    }

    [Fact]
    public async Task PublishDraft_ShouldRejectSensitiveData()
    {
        await using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var userId = Guid.NewGuid();
        var currentUserContext = CreateCurrentUserContext(userId);
        var activityLogger = new Mock<IActivityLogger>();
        var imageStore = new Mock<IPublicAnnouncementImageStore>();
        var renderer = new PublicAnnouncementRenderer();
        var publisher = new NullPublicAnnouncementPublisher(NullLogger<NullPublicAnnouncementPublisher>.Instance);

        var createHandler = new CreatePublicAnnouncementDraftCommandHandler(unitOfWork, currentUserContext.Object, renderer, activityLogger.Object);
        var transitionHandler = new TransitionPublicAnnouncementDraftCommandHandler(unitOfWork, currentUserContext.Object, activityLogger.Object, imageStore.Object);
        var publishHandler = new PublishPublicAnnouncementDraftCommandHandler(unitOfWork, currentUserContext.Object, renderer, publisher, activityLogger.Object, imageStore.Object);

        var created = await createHandler.Handle(
            new CreatePublicAnnouncementDraftCommand(
                "Release",
                "Bearer secret-token",
                PublicAnnouncementSourceType.Manual,
                null,
                null,
                null,
                null,
                null,
                PublicAnnouncementParseMode.PlainText,
                null,
                null,
                null,
                PublicAnnouncementImagePlacement.ImageFirst,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(ResultType.BadRequest, created.Type);
        Assert.Equal("DraftContainsSensitiveData", created.ErrorCode);

        var draft = new PublicAnnouncementDraft
        {
            Title = "Release",
            Body = "Bearer secret-token",
            ParseMode = PublicAnnouncementParseMode.PlainText,
            Status = PublicAnnouncementStatus.Approved,
            CreatedByUserKey = userId,
            UpdatedByUserKey = userId
        };

        context.PublicAnnouncementDrafts.Add(draft);
        await context.SaveChangesAsync();

        var publish = await publishHandler.Handle(new PublishPublicAnnouncementDraftCommand(draft.PublicAnnouncementDraftKey), CancellationToken.None);

        Assert.Equal(ResultType.BadRequest, publish.Type);
        Assert.Equal("DraftContainsSensitiveData", publish.ErrorCode);
    }

    [Fact]
    public void Renderer_WhenImageCaptionIsTooLong_ShouldWarnAboutSeparateTextMessage()
    {
        var renderer = new PublicAnnouncementRenderer();
        var draft = new PublicAnnouncementDraft
        {
            Title = "Release",
            Body = new string('a', 1100),
            ImageUrl = "https://example.com/release.png"
        };

        var preview = renderer.Render(draft);

        Assert.True(preview.WillSendAsPhoto);
        Assert.True(preview.RequiresSeparateTextMessage);
        Assert.Contains(preview.Warnings, warning => warning.Contains("caption", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Renderer_WhenMarkdownV2_ShouldEscapeTitleCharacters()
    {
        var renderer = new PublicAnnouncementRenderer();
        var draft = new PublicAnnouncementDraft
        {
            Title = "Release [v1.0] (Hotfix)!",
            Body = "Body stays raw.",
            ParseMode = PublicAnnouncementParseMode.MarkdownV2
        };

        var preview = renderer.Render(draft);

        Assert.Equal("*Release \\[v1\\.0\\] \\(Hotfix\\)\\!*\n\nBody stays raw.", preview.RenderedText);
        Assert.Equal(PublicAnnouncementParseMode.MarkdownV2, preview.ParseMode);
    }

    [Fact]
    public void Renderer_WhenHtml_ShouldEncodeTitleOnly()
    {
        var renderer = new PublicAnnouncementRenderer();
        var draft = new PublicAnnouncementDraft
        {
            Title = "<b>Release</b>",
            Body = "Body can include <i>tags</i>.",
            ParseMode = PublicAnnouncementParseMode.Html
        };

        var preview = renderer.Render(draft);

        Assert.Equal("<b>&lt;b&gt;Release&lt;/b&gt;</b>\n\nBody can include <i>tags</i>.", preview.RenderedText);
    }

    [Fact]
    public void Renderer_WhenTextIsTooLong_ShouldWarnAboutLimit()
    {
        var renderer = new PublicAnnouncementRenderer();
        var draft = new PublicAnnouncementDraft
        {
            Title = "Release",
            Body = new string('a', 5000),
            ParseMode = PublicAnnouncementParseMode.PlainText
        };

        var preview = renderer.Render(draft);

        Assert.Contains(preview.Warnings, warning => warning.Contains("exceeds Telegram message limit", StringComparison.OrdinalIgnoreCase));
        Assert.False(preview.RequiresSeparateTextMessage);
    }

    [Fact]
    public async Task TelegramPublisher_ForTextDraft_ShouldSendMessage()
    {
        var handler = new CapturingTelegramHandler();
        var publisher = CreateTelegramPublisher(handler);
        var draft = new PublicAnnouncementDraft
        {
            Title = "Release",
            Body = "ProjectK updated.",
            ParseMode = PublicAnnouncementParseMode.Html
        };

        var result = await publisher.PublishAsync(draft, "<b>Release</b>\n\nProjectK updated.", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("101", result.TelegramMessageId);
        var request = Assert.Single(handler.Requests);
        Assert.EndsWith("/bottest-token/sendMessage", request.Url);
        Assert.Contains("\"chat_id\":\"@projectk_public\"", request.Body);
        Assert.Contains("\"parse_mode\":\"HTML\"", request.Body);
        Assert.Contains("ProjectK updated.", request.Body);
    }

    [Fact]
    public async Task TelegramPublisher_WhenDryRun_ShouldNotSendRequests()
    {
        var handler = new CapturingTelegramHandler();
        var publisher = CreateTelegramPublisher(handler, dryRun: true);
        var draft = new PublicAnnouncementDraft
        {
            Title = "Release",
            Body = "ProjectK updated.",
            ParseMode = PublicAnnouncementParseMode.PlainText
        };

        var result = await publisher.PublishAsync(draft, "Release\n\nProjectK updated.", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.StartsWith("dry-run:", result.TelegramMessageId);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task TelegramPublisher_ForImageWithLongCaption_ShouldSendPhotoThenText()
    {
        var handler = new CapturingTelegramHandler();
        var publisher = CreateTelegramPublisher(handler);
        var draft = new PublicAnnouncementDraft
        {
            Title = "Release",
            Body = new string('a', 1100),
            ImageUrl = "https://example.com/release.png",
            ParseMode = PublicAnnouncementParseMode.PlainText
        };

        var result = await publisher.PublishAsync(draft, $"Release\n\n{draft.Body}", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("101,102", result.TelegramMessageId);
        Assert.Equal(2, handler.Requests.Count);
        Assert.EndsWith("/bottest-token/sendPhoto", handler.Requests[0].Url);
        Assert.Contains("\"photo\":\"https://example.com/release.png\"", handler.Requests[0].Body);
        Assert.Contains("\"caption\":null", handler.Requests[0].Body);
        Assert.EndsWith("/bottest-token/sendMessage", handler.Requests[1].Url);
    }

    [Fact]
    public async Task TelegramPublisher_ForUploadedImage_ShouldSendMultipartPhoto()
    {
        var handler = new CapturingTelegramHandler();
        var imageStore = new Mock<IPublicAnnouncementImageStore>();
        imageStore
            .Setup(x => x.OpenAsync("announcement-image.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublicAnnouncementImageFile(
                new MemoryStream([0x01, 0x02, 0x03]),
                "announcement-image.jpg",
                "image/jpeg"));

        var publisher = CreateTelegramPublisher(handler, imageStore.Object);
        var draft = new PublicAnnouncementDraft
        {
            Title = "Release",
            Body = "ProjectK updated.",
            ImageBlobKey = "announcement-image.jpg",
            ImageUrl = "https://api.local/admin-preview-url",
            ParseMode = PublicAnnouncementParseMode.Html
        };

        var result = await publisher.PublishAsync(draft, "<b>Release</b>\n\nProjectK updated.", CancellationToken.None);

        Assert.True(result.Succeeded);
        var request = Assert.Single(handler.Requests);
        Assert.EndsWith("/bottest-token/sendPhoto", request.Url);
        Assert.Equal("multipart/form-data", request.ContentType);
        Assert.Contains("name=photo", request.Body);
        Assert.DoesNotContain("api.local", request.Body);
    }

    [Fact]
    public async Task TelegramPublisher_ForImageLast_ShouldSendSinglePhotoWithCaptionAboveMedia()
    {
        var handler = new CapturingTelegramHandler();
        var publisher = CreateTelegramPublisher(handler);
        var draft = new PublicAnnouncementDraft
        {
            Title = "Release",
            Body = "ProjectK updated.",
            ImageUrl = "https://example.com/release.png",
            ImagePlacement = PublicAnnouncementImagePlacement.ImageLast,
            ParseMode = PublicAnnouncementParseMode.Html
        };

        var result = await publisher.PublishAsync(draft, "<b>Release</b>\n\nProjectK updated.", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("101", result.TelegramMessageId);
        var request = Assert.Single(handler.Requests);
        Assert.EndsWith("/bottest-token/sendPhoto", request.Url);
        Assert.Contains("\"photo\":\"https://example.com/release.png\"", request.Body);
        Assert.Contains("\"caption\":", request.Body);
        Assert.Contains("ProjectK updated.", request.Body);
        Assert.Contains("\"show_caption_above_media\":true", request.Body);
    }

    [Fact]
    public async Task TelegramPublisher_ForImageLastAndLongText_ShouldFallbackToTextThenPhoto()
    {
        var handler = new CapturingTelegramHandler();
        var publisher = CreateTelegramPublisher(handler);
        var draft = new PublicAnnouncementDraft
        {
            Title = "Release",
            Body = "ProjectK updated.",
            ImageUrl = "https://example.com/release.png",
            ImagePlacement = PublicAnnouncementImagePlacement.ImageLast,
            ParseMode = PublicAnnouncementParseMode.Html
        };

        var result = await publisher.PublishAsync(draft, new string('a', 1025), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("101,102", result.TelegramMessageId);
        Assert.Equal(2, handler.Requests.Count);
        Assert.EndsWith("/bottest-token/sendMessage", handler.Requests[0].Url);
        Assert.EndsWith("/bottest-token/sendPhoto", handler.Requests[1].Url);
        Assert.Contains("\"photo\":\"https://example.com/release.png\"", handler.Requests[1].Body);
        Assert.Contains("\"caption\":null", handler.Requests[1].Body);
        Assert.Contains("\"show_caption_above_media\":false", handler.Requests[1].Body);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Mock<ICurrentUserContext> CreateCurrentUserContext(Guid userId)
    {
        var mock = new Mock<ICurrentUserContext>();
        mock.Setup(x => x.IsAuthenticated).Returns(true);
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.Roles).Returns(new[] { UserRole.Admin.ToString() });
        mock.Setup(x => x.IsInRole(UserRole.Admin.ToString())).Returns(true);
        return mock;
    }

    private static TelegramPublicAnnouncementPublisher CreateTelegramPublisher(
        CapturingTelegramHandler handler,
        IPublicAnnouncementImageStore? imageStore = null,
        bool dryRun = false)
    {
        var options = Options.Create(new TelegramOptions
        {
            PublicChannel = new TelegramChannelOptions
            {
                Enabled = true,
                DryRun = dryRun,
                BotToken = "test-token",
                ChatId = "@projectk_public",
                BaseUrl = "https://telegram.test",
                TimeoutSeconds = 10
            }
        });

        return new TelegramPublicAnnouncementPublisher(
            new HttpClient(handler),
            options,
            NullLogger<TelegramPublicAnnouncementPublisher>.Instance,
            imageStore ?? Mock.Of<IPublicAnnouncementImageStore>());
    }

    private sealed class CapturingTelegramHandler : HttpMessageHandler
    {
        private int _messageId = 100;

        public List<(string Url, string Body, string? ContentType)> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add((request.RequestUri!.ToString(), body, request.Content?.Headers.ContentType?.MediaType));

            var messageId = Interlocked.Increment(ref _messageId);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"ok\":true,\"result\":{{\"message_id\":{messageId}}}}}",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
