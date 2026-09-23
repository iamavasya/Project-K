using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.SubmitWaitlistRegistration;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests.Onboarding;

public class SubmitWaitlistRegistrationHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMemberDirectory> _memberDirectory = new();
    private readonly Mock<IWaitlistRepository> _waitlistRepository = new();
    private readonly Mock<IMemberRepository> _memberRepository = new();
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly AppUser _admin = new() { Id = Guid.NewGuid(), Email = "admin@example.com", UserName = "admin@example.com" };
    private readonly SubmitWaitlistRegistrationCommandHandler _handler;

    public SubmitWaitlistRegistrationHandlerTests()
    {
        _unitOfWork.Setup(x => x.WaitlistEntries).Returns(_waitlistRepository.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _waitlistRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaitlistEntry?)null);
        _memberRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        _userManager = new Mock<UserManager<AppUser>>(
            new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);
        _userManager
            .Setup(x => x.GetUsersInRoleAsync(SystemRole.Admin))
            .ReturnsAsync(new List<AppUser> { _admin });

        _handler = new SubmitWaitlistRegistrationCommandHandler(
            _unitOfWork.Object,
            _memberDirectory.Object,
            _userManager.Object,
            _emailService.Object,
            _notifications.Object,
            NullLogger<SubmitWaitlistRegistrationCommandHandler>.Instance);
    }

    // Nobody reads the waitlist page on a schedule: the entry has to come to the administrator.
    [Fact]
    public async Task Handle_ShouldTellEveryAdministrator_ByBellAndByLetter()
    {
        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.Type.Should().Be(ResultType.Created);
        _notifications.Verify(x => x.NotifyManyAsync(
            It.Is<IEnumerable<NotificationRequest>>(requests => requests.Single().RecipientUserKey == _admin.Id
                && requests.Single().Type == AppNotificationType.WaitlistEntrySubmitted
                && requests.Single().Route == "/waitlist"
                && requests.Single().Body == "Ihor Kovalenko · курінь 97"),
            It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(x => x.SendWaitlistSubmittedEmailAsync("admin@example.com", "Ihor Kovalenko", "97", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldStillCreateTheEntry_WhenTellingAdministratorsFails()
    {
        _emailService
            .Setup(x => x.SendWaitlistSubmittedEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("mail is down"));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.Type.Should().Be(ResultType.Created);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPersistStanytsiaAndRegionOrCountry_WhenRegistrationIsValid()
    {
        WaitlistEntry? capturedEntry = null;
        _waitlistRepository
            .Setup(x => x.Create(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()))
            .Callback<WaitlistEntry, CancellationToken>((entry, _) => capturedEntry = entry);

        var command = CreateCommand(stanytsia: "  Kyiv  ", regionOrCountry: "  Ukraine  ");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Type.Should().Be(ResultType.Created);
        capturedEntry.Should().NotBeNull();
        capturedEntry!.Stanytsia.Should().Be("Kyiv");
        capturedEntry.RegionOrCountry.Should().Be("Ukraine");
        capturedEntry.IsKurinLeaderCandidate.Should().BeTrue();
        capturedEntry.ClaimedKurinNameOrNumber.Should().Be("97");
        capturedEntry.VerificationStatus.Should().Be(WaitlistVerificationStatus.Submitted);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SubmitWaitlistRegistrationCommand CreateCommand(
        string? stanytsia = "Kyiv",
        string? regionOrCountry = "Ukraine",
        bool isKurinLeaderCandidate = true,
        string? claimedKurinNumber = "97")
    {
        return new SubmitWaitlistRegistrationCommand(
            "Ihor",
            "Kovalenko",
            "ihor.kovalenko@example.com",
            "+38 (099) 111-22-33",
            new DateTime(1995, 5, 15),
            stanytsia,
            regionOrCountry,
            isKurinLeaderCandidate,
            claimedKurinNumber);
    }
}
