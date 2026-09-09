using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ResendInvitation;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests.Onboarding;

public sealed class ResendInvitationByEmailHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInvitationRepository> _invitations = new();
    private readonly Mock<IWaitlistRepository> _waitlistEntries = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly ResendInvitationByEmailHandler _handler;

    public ResendInvitationByEmailHandlerTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Invitations).Returns(_invitations.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.WaitlistEntries).Returns(_waitlistEntries.Object);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new ResendInvitationByEmailHandler(
            _userManager.Object,
            _unitOfWork.Object,
            _emailService.Object,
            TimeProvider.System);
    }

    [Fact]
    public async Task SendsInvitationForPendingActivationUser()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "pending@example.com",
            OnboardingStatus = OnboardingStatus.PendingActivation
        };
        var entry = new WaitlistEntry
        {
            WaitlistEntryKey = Guid.NewGuid(),
            Email = user.Email
        };

        _userManager.Setup(manager => manager.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _waitlistEntries.Setup(repository => repository.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        _invitations.Setup(repository => repository.GetActiveByWaitlistEntryKeyAsync(entry.WaitlistEntryKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invitation?)null);

        var result = await _handler.Handle(new ResendInvitationByEmailCommand(user.Email), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(service => service.SendInvitationEmailAsync(
            user.Email,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DoesNotSendInvitationForActiveUser()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "active@example.com",
            OnboardingStatus = OnboardingStatus.Active
        };
        _userManager.Setup(manager => manager.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _handler.Handle(new ResendInvitationByEmailCommand(user.Email), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        _emailService.Verify(service => service.SendInvitationEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DoesNotRevealUnknownUser()
    {
        _userManager.Setup(manager => manager.FindByEmailAsync("unknown@example.com"))
            .ReturnsAsync((AppUser?)null);

        var result = await _handler.Handle(
            new ResendInvitationByEmailCommand("unknown@example.com"),
            CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        result.Data.Should().BeTrue();
        _emailService.Verify(service => service.SendInvitationEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
