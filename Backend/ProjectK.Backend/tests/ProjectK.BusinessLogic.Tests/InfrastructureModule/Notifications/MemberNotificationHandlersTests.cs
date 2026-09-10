using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Notifications;
using ProjectK.BusinessLogic.Services.Events;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Events;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.InfrastructureModule.Notifications;

/// <summary>
/// The wording, the severity and the route now live with the inbox rather than with the use case
/// that raised the event, so this is where they are pinned.
/// </summary>
public class MemberNotificationHandlersTests
{
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<IReviewNotificationRecipientResolver> _recipients = new();

    private static DomainEventNotification<TEvent> Raised<TEvent>(TEvent domainEvent)
        where TEvent : ProjectK.Common.Interfaces.IDomainEvent
        => new(domainEvent);


    [Fact]
    public async Task StaleProfile_ShouldAskTheOwnerToHaveItCheckedAgain()
    {
        var memberKey = Guid.NewGuid();
        var ownerUserKey = Guid.NewGuid();
        var actorUserKey = Guid.NewGuid();
        var handler = new MemberProfileWentStaleNotificationHandler(_notifications.Object);

        await handler.Handle(
            Raised(new MemberProfileWentStale(memberKey, ownerUserKey, actorUserKey)),
            CancellationToken.None);

        _notifications.Verify(x => x.NotifyAsync(
            It.Is<NotificationRequest>(request =>
                request.RecipientUserKey == ownerUserKey
                && request.Type == AppNotificationType.MemberProfileChangedAfterVerification
                && request.Severity == AppNotificationSeverity.Warn
                && request.EntityType == "Member"
                && request.EntityKey == memberKey
                && request.Route == $"/member/{memberKey}"
                && request.ActorUserKey == actorUserKey
                && request.DeduplicationKey == $"member-profile-stale:{memberKey}"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifiedProfile_ShouldTellTheOwnerItIsCurrent()
    {
        var memberKey = Guid.NewGuid();
        var ownerUserKey = Guid.NewGuid();
        var handler = new MemberProfileVerifiedNotificationHandler(_notifications.Object);

        await handler.Handle(
            Raised(new MemberProfileVerified(memberKey, ownerUserKey, null)),
            CancellationToken.None);

        _notifications.Verify(x => x.NotifyAsync(
            It.Is<NotificationRequest>(request =>
                request.RecipientUserKey == ownerUserKey
                && request.Type == AppNotificationType.MemberProfileVerified
                && request.Severity == AppNotificationSeverity.Success
                && request.Route == $"/member/{memberKey}"
                && request.DeduplicationKey == $"member-profile-verified:{memberKey}"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(true, AppNotificationSeverity.Success, "Відзначення затверджено")]
    [InlineData(false, AppNotificationSeverity.Warn, "Відзначення не затверджено")]
    public async Task ReviewedAward_ShouldMatchTheOutcome(
        bool isApproved,
        AppNotificationSeverity severity,
        string title)
    {
        var awardKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        var ownerUserKey = Guid.NewGuid();
        var handler = new MemberAwardReviewedNotificationHandler(_notifications.Object);

        await handler.Handle(
            Raised(new MemberAwardReviewed(awardKey, memberKey, ownerUserKey, isApproved, null)),
            CancellationToken.None);

        _notifications.Verify(x => x.NotifyAsync(
            It.Is<NotificationRequest>(request =>
                request.RecipientUserKey == ownerUserKey
                && request.Severity == severity
                && request.Title == title
                && request.EntityKey == awardKey
                && request.Route == $"/member/{memberKey}"
                && request.DeduplicationKey == $"award-review:{awardKey}"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmittedAward_ShouldReachEveryReviewerTheResolverNames()
    {
        var awardKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        var kurinKey = Guid.NewGuid();
        var groupKey = Guid.NewGuid();
        var actorUserKey = Guid.NewGuid();
        var managerUserKey = Guid.NewGuid();
        var mentorUserKey = Guid.NewGuid();

        _recipients
            .Setup(x => x.ResolveAsync(kurinKey, groupKey, actorUserKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync([managerUserKey, mentorUserKey]);

        var handler = new MemberAwardSubmittedNotificationHandler(_notifications.Object, _recipients.Object);

        await handler.Handle(
            Raised(new MemberAwardSubmitted(awardKey, memberKey, "Іван Петренко", kurinKey, groupKey, actorUserKey)),
            CancellationToken.None);

        _notifications.Verify(x => x.NotifyManyAsync(
            It.Is<IEnumerable<NotificationRequest>>(requests =>
                requests.Count() == 2
                && requests.Select(request => request.RecipientUserKey)
                    .ToHashSet()
                    .SetEquals(new[] { managerUserKey, mentorUserKey })
                && requests.All(request =>
                    request.Type == AppNotificationType.MemberAwardSubmitted
                    && request.Title == "Відзначення подано на розгляд"
                    && request.Body == "Надійшло відзначення від Іван Петренко на розгляд."
                    && request.EntityKey == awardKey
                    && request.DeduplicationKey == $"award-submitted:{awardKey}")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmittedAward_WithNoReviewers_ShouldSendNothing()
    {
        _recipients
            .Setup(x => x.ResolveAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new MemberAwardSubmittedNotificationHandler(_notifications.Object, _recipients.Object);

        await handler.Handle(
            Raised(new MemberAwardSubmitted(Guid.NewGuid(), Guid.NewGuid(), "X", Guid.NewGuid(), null, null)),
            CancellationToken.None);

        _notifications.Verify(x => x.NotifyManyAsync(
            It.IsAny<IEnumerable<NotificationRequest>>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(MemberWarningLevel.Level1, "першу пересторогу")]
    [InlineData(MemberWarningLevel.Level2, "другу пересторогу")]
    [InlineData(MemberWarningLevel.Level3, "третю пересторогу")]
    public async Task AssignedWarning_ShouldNameTheLevel(MemberWarningLevel level, string expected)
    {
        var warningKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        var ownerUserKey = Guid.NewGuid();
        var handler = new MemberWarningAssignedNotificationHandler(_notifications.Object);

        await handler.Handle(
            Raised(new MemberWarningAssigned(warningKey, memberKey, ownerUserKey, level, null)),
            CancellationToken.None);

        _notifications.Verify(x => x.NotifyAsync(
            It.Is<NotificationRequest>(request =>
                request.RecipientUserKey == ownerUserKey
                && request.Severity == AppNotificationSeverity.Warn
                && request.Body == $"До вашого профілю додано {expected}."
                && request.EntityKey == warningKey
                && request.DeduplicationKey == $"member-warning:{warningKey}"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmittedBadge_ShouldPointReviewersAtTheKurinQueue()
    {
        var memberKey = Guid.NewGuid();
        var kurinKey = Guid.NewGuid();
        var mentorUserKey = Guid.NewGuid();

        _recipients
            .Setup(x => x.ResolveAsync(kurinKey, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([mentorUserKey]);

        var handler = new BadgeProgressSubmittedNotificationHandler(_notifications.Object, _recipients.Object);

        await handler.Handle(
            Raised(new BadgeProgressSubmitted(Guid.NewGuid(), "badge-1", memberKey, "Іван", kurinKey, null, null)),
            CancellationToken.None);

        _notifications.Verify(x => x.NotifyManyAsync(
            It.Is<IEnumerable<NotificationRequest>>(requests =>
                requests.Single().RecipientUserKey == mentorUserKey
                && requests.Single().Type == AppNotificationType.MemberSkillSubmittedForReview
                && requests.Single().Route == $"/kurin/{kurinKey}/review/skills"
                && requests.Single().DeduplicationKey == $"skill-review:{memberKey}:badge-1"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(true, false, "Вмілість зараховано")]
    [InlineData(false, true, "Підтвердження вмілості скасовано")]
    [InlineData(false, false, "Вмілість потребує доопрацювання")]
    public async Task ReviewedBadge_ShouldSeparateRejectionFromWithdrawal(
        bool isApproved,
        bool withdrawn,
        string title)
    {
        var memberKey = Guid.NewGuid();
        var ownerUserKey = Guid.NewGuid();
        var handler = new BadgeProgressReviewedNotificationHandler(_notifications.Object);

        await handler.Handle(
            Raised(new BadgeProgressReviewed(
                Guid.NewGuid(), "badge-1", memberKey, ownerUserKey, isApproved, withdrawn, null)),
            CancellationToken.None);

        _notifications.Verify(x => x.NotifyAsync(
            It.Is<NotificationRequest>(request =>
                request.RecipientUserKey == ownerUserKey
                && request.Title == title
                && request.Route == $"/member/{memberKey}"
                && request.DeduplicationKey == $"skill-review-result:{memberKey}:badge-1"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
