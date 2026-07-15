using AutoMapper;
using FluentAssertions;
using MediatR;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MentorAssignmentEntity = ProjectK.Common.Entities.KurinModule.MentorAssignment;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MemberAwardHandlers;

public class MemberAwardHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IMemberAwardRepository> _memberAwardRepositoryMock;
    private readonly Mock<ICurrentUserContext> _currentUserContextMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IReviewNotificationRecipientResolver> _recipientResolverMock;
    private readonly Mock<IMapper> _mapperMock;

    private readonly UpsertMemberAwardHandler _upsertHandler;
    private readonly ReviewMemberAwardHandler _reviewHandler;
    private readonly DeleteMemberAwardHandler _deleteHandler;

    public MemberAwardHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _memberAwardRepositoryMock = new Mock<IMemberAwardRepository>();
        _currentUserContextMock = new Mock<ICurrentUserContext>();
        _notificationServiceMock = new Mock<INotificationService>();
        _recipientResolverMock = new Mock<IReviewNotificationRecipientResolver>();
        _mapperMock = new Mock<IMapper>();

        _unitOfWorkMock.SetupGet(x => x.Members).Returns(_memberRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.MemberAwards).Returns(_memberAwardRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _recipientResolverMock
            .Setup(x => x.ResolveAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        _currentUserContextMock.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

        _upsertHandler = new UpsertMemberAwardHandler(
            _unitOfWorkMock.Object,
            _currentUserContextMock.Object,
            _notificationServiceMock.Object,
            _recipientResolverMock.Object,
            _mapperMock.Object);
        _reviewHandler = new ReviewMemberAwardHandler(
            _unitOfWorkMock.Object,
            _currentUserContextMock.Object,
            _notificationServiceMock.Object,
            _mapperMock.Object);
        _deleteHandler = new DeleteMemberAwardHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Upsert_ShouldNotifyKurinManagersAndActiveGroupMentors()
    {
        var actorUserKey = Guid.NewGuid();
        var managerUserKey = Guid.NewGuid();
        var mentorUserKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        var kurinKey = Guid.NewGuid();
        var groupKey = Guid.NewGuid();

        _currentUserContextMock.SetupGet(x => x.UserId).Returns(actorUserKey);
        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member
            {
                MemberKey = memberKey,
                KurinKey = kurinKey,
                GroupKey = groupKey,
                FirstName = "Ivan",
                LastName = "Petrenko"
            });
        _recipientResolverMock
            .Setup(x => x.ResolveAsync(kurinKey, groupKey, actorUserKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { managerUserKey, mentorUserKey });

        MemberAward? created = null;
        _memberAwardRepositoryMock
            .Setup(x => x.Create(It.IsAny<MemberAward>(), It.IsAny<CancellationToken>()))
            .Callback<MemberAward, CancellationToken>((award, _) => created = award);
        _mapperMock.Setup(m => m.Map<MemberAwardDto>(It.IsAny<MemberAward>())).Returns(new MemberAwardDto());

        var result = await _upsertHandler.Handle(new UpsertMemberAward
        {
            MemberKey = memberKey,
            Level = MemberAwardLevel.First,
            DateAcquired = DateTime.UtcNow
        }, CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        created.Should().NotBeNull();
        _notificationServiceMock.Verify(x => x.NotifyManyAsync(
            It.Is<IEnumerable<NotificationRequest>>(requests =>
                requests.Count() == 2
                && requests.Select(request => request.RecipientUserKey)
                    .ToHashSet()
                    .SetEquals(new[] { managerUserKey, mentorUserKey })
                && requests.All(request =>
                    request.Type == AppNotificationType.MemberAwardSubmitted
                    && request.Severity == AppNotificationSeverity.Info
                    && request.Title == "Відзначення подано на розгляд"
                    && request.EntityType == "MemberAward"
                    && request.EntityKey == created!.MemberAwardKey
                    && request.Route == $"/member/{memberKey}"
                    && request.ActorUserKey == actorUserKey
                    && request.DeduplicationKey == $"award-submitted:{created.MemberAwardKey}")),
            It.IsAny<CancellationToken>()),
            Times.Once);
        _recipientResolverMock.Verify(x => x.ResolveAsync(
            kurinKey,
            groupKey,
            actorUserKey,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Upsert_ShouldCreateNewAward_WhenKeyIsNotProvided()
    {
        var memberKey = Guid.NewGuid();
        var kurinKey = Guid.NewGuid();
        var userKey = _currentUserContextMock.Object.UserId!.Value;
        var dateAcquired = DateTime.UtcNow.AddDays(-1);

        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = kurinKey });

        MemberAward? created = null;
        _memberAwardRepositoryMock
            .Setup(x => x.Create(It.IsAny<MemberAward>(), It.IsAny<CancellationToken>()))
            .Callback<MemberAward, CancellationToken>((award, _) => created = award);

        _mapperMock.Setup(m => m.Map<MemberAwardDto>(It.IsAny<MemberAward>())).Returns(new MemberAwardDto());

        var result = await _upsertHandler.Handle(new UpsertMemberAward 
        { 
            MemberKey = memberKey, 
            Level = MemberAwardLevel.First, 
            DateAcquired = dateAcquired, 
            Note = "Test" 
        }, CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        created.Should().NotBeNull();
        created!.MemberKey.Should().Be(memberKey);
        created.KurinKey.Should().Be(kurinKey);
        created.Level.Should().Be(MemberAwardLevel.First);
        created.DateAcquired.Should().Be(dateAcquired);
        created.Note.Should().Be("Test");
        created.Status.Should().Be(BadgeProgressStatus.Submitted);
        created.SubmittedByUserKey.Should().Be(userKey);
        created.SubmittedAtUtc.Should().NotBeNull();
        created.ReviewedByUserKey.Should().BeNull();
        created.ReviewedAtUtc.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upsert_ShouldUpdateExistingAward_WhenKeyIsProvided()
    {
        var memberKey = Guid.NewGuid();
        var kurinKey = Guid.NewGuid();
        var awardKey = Guid.NewGuid();
        var userKey = _currentUserContextMock.Object.UserId!.Value;
        var dateAcquired = DateTime.UtcNow.AddDays(-1);

        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = kurinKey });

        var existingAward = new MemberAward 
        {
            MemberAwardKey = awardKey,
            MemberKey = memberKey,
            KurinKey = kurinKey,
            Level = MemberAwardLevel.First,
            DateAcquired = DateTime.UtcNow.AddDays(-10),
            Note = "Old Note",
            Status = BadgeProgressStatus.Confirmed,
            ReviewedByUserKey = Guid.NewGuid(),
            ReviewedAtUtc = DateTime.UtcNow.AddDays(-5)
        };

        _memberAwardRepositoryMock
            .Setup(x => x.GetByKeyAsync(awardKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAward);

        _mapperMock.Setup(m => m.Map<MemberAwardDto>(It.IsAny<MemberAward>())).Returns(new MemberAwardDto());

        var result = await _upsertHandler.Handle(new UpsertMemberAward 
        { 
            MemberAwardKey = awardKey,
            MemberKey = memberKey, 
            Level = MemberAwardLevel.First, 
            DateAcquired = dateAcquired, 
            Note = "New Note" 
        }, CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        
        _memberAwardRepositoryMock.Verify(x => x.Update(existingAward, It.IsAny<CancellationToken>()), Times.Once);
        
        existingAward.DateAcquired.Should().Be(dateAcquired);
        existingAward.Note.Should().Be("New Note");
        existingAward.Status.Should().Be(BadgeProgressStatus.Submitted);
        existingAward.SubmittedByUserKey.Should().Be(userKey);
        existingAward.SubmittedAtUtc.Should().NotBeNull();
        existingAward.ReviewedByUserKey.Should().BeNull();
        existingAward.ReviewedAtUtc.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Review_ShouldReturnConflict_WhenTryingToRemoveConfirmedAward()
    {
        var awardKey = Guid.NewGuid();
        var existingAward = new MemberAward
        {
            MemberAwardKey = awardKey,
            Status = BadgeProgressStatus.Confirmed,
            ReviewedAtUtc = DateTime.UtcNow.AddDays(-1),
            ReviewedByUserKey = Guid.NewGuid()
        };

        _memberAwardRepositoryMock
            .Setup(x => x.GetByKeyAsync(awardKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAward);

        var result = await _reviewHandler.Handle(new ReviewMemberAward
        {
            MemberAwardKey = awardKey,
            IsApproved = false
        }, CancellationToken.None);

        result.Type.Should().Be(ResultType.Conflict);
        existingAward.Status.Should().Be(BadgeProgressStatus.Confirmed);
        _memberAwardRepositoryMock.Verify(x => x.Update(It.IsAny<MemberAward>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Review_LinkedMemberAward_ShouldNotifyMemberOwner()
    {
        var actorUserKey = Guid.NewGuid();
        var memberUserKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        var awardKey = Guid.NewGuid();
        var award = new MemberAward
        {
            MemberAwardKey = awardKey,
            MemberKey = memberKey,
            Status = BadgeProgressStatus.Submitted
        };

        _currentUserContextMock.SetupGet(x => x.UserId).Returns(actorUserKey);
        _memberAwardRepositoryMock
            .Setup(x => x.GetByKeyAsync(awardKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(award);
        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, UserKey = memberUserKey });
        _mapperMock.Setup(m => m.Map<MemberAwardDto>(It.IsAny<MemberAward>())).Returns(new MemberAwardDto());

        var result = await _reviewHandler.Handle(new ReviewMemberAward
        {
            MemberAwardKey = awardKey,
            IsApproved = true
        }, CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        _notificationServiceMock.Verify(x => x.NotifyAsync(
            It.Is<NotificationRequest>(request =>
                request.RecipientUserKey == memberUserKey
                && request.Type == AppNotificationType.MemberAwardReviewed
                && request.Severity == AppNotificationSeverity.Success
                && request.EntityKey == awardKey
                && request.Route == $"/member/{memberKey}"
                && request.ActorUserKey == actorUserKey
                && request.DeduplicationKey == $"award-review:{awardKey}"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldDeleteAward_WhenItExists()
    {
        var awardKey = Guid.NewGuid();
        var existingAward = new MemberAward { MemberAwardKey = awardKey };

        _memberAwardRepositoryMock
            .Setup(x => x.GetByKeyAsync(awardKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAward);

        var result = await _deleteHandler.Handle(new DeleteMemberAward { MemberAwardKey = awardKey }, CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        _memberAwardRepositoryMock.Verify(x => x.Delete(existingAward, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenItDoesNotExist()
    {
        var awardKey = Guid.NewGuid();

        _memberAwardRepositoryMock
            .Setup(x => x.GetByKeyAsync(awardKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberAward?)null);

        var result = await _deleteHandler.Handle(new DeleteMemberAward { MemberAwardKey = awardKey }, CancellationToken.None);

        result.Type.Should().Be(ResultType.NotFound);
        _memberAwardRepositoryMock.Verify(x => x.Delete(It.IsAny<MemberAward>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
