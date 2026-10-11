using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ApproveWaitlistEntry;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Settings;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests.Onboarding;

public class ApproveWaitlistEntryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IWaitlistRepository> _waitlist = new();
    private readonly Mock<IKurinRepository> _kurins = new();
    private readonly Mock<IAccountProvisioningService> _provisioning = new();
    private readonly ApproveWaitlistEntryCommandHandler _handler;
    private Kurin? _openedKurin;

    public ApproveWaitlistEntryHandlerTests()
    {
        _unitOfWork.Setup(x => x.WaitlistEntries).Returns(_waitlist.Object);
        _unitOfWork.Setup(x => x.Kurins).Returns(_kurins.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _kurins
            .Setup(x => x.Create(It.IsAny<Kurin>(), It.IsAny<CancellationToken>()))
            .Callback<Kurin, CancellationToken>((kurin, _) => _openedKurin = kurin);
        _provisioning
            .Setup(x => x.ProvisionAsync(It.IsAny<AccountProvisioningRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceResult<AccountProvisioningResult>(
                ResultType.Success, new AccountProvisioningResult(Guid.NewGuid(), Guid.NewGuid(), "token")));

        var userManager = new Mock<UserManager<AppUser>>(
            new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);

        _handler = new ApproveWaitlistEntryCommandHandler(
            _unitOfWork.Object,
            userManager.Object,
            _provisioning.Object,
            new Mock<IEmailService>().Object,
            new Mock<ICurrentUserContext>().Object,
            new Mock<IMembershipDirectory>().Object,
            Options.Create(new KurinNumberOptions()));
    }

    [Fact]
    public async Task Handle_ShouldOpenTheClaimedNumber_WhenItIsFree()
    {
        var entry = Given(claimed: "47");

        var result = await _handler.Handle(new ApproveWaitlistEntryCommand(entry.WaitlistEntryKey), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        _openedKurin!.Number.Should().Be(47);
        entry.FoundedKurinKey.Should().Be(_openedKurin.KurinKey);
    }

    [Fact]
    public async Task Handle_ShouldOpenAPreparatoryKurin_WhenTheApplicantLeadsOne()
    {
        var entry = Given(claimed: null, preparatory: true);

        await _handler.Handle(new ApproveWaitlistEntryCommand(entry.WaitlistEntryKey), CancellationToken.None);

        _openedKurin!.Number.Should().BeNull();
    }

    // The first outside applicant wrote "0" before the form knew about підготовчі курені.
    [Theory]
    [InlineData("0")]
    [InlineData("1153")]
    public async Task Handle_ShouldOpenAPreparatoryKurin_WhenTheClaimIsOutsideTheRange(string claimed)
    {
        var entry = Given(claimed);

        await _handler.Handle(new ApproveWaitlistEntryCommand(entry.WaitlistEntryKey), CancellationToken.None);

        _openedKurin!.Number.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldRefuse_WhenTheClaimedNumberIsTaken()
    {
        var entry = Given(claimed: "47");
        _kurins.Setup(x => x.ExistsAsync(47, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.Handle(new ApproveWaitlistEntryCommand(entry.WaitlistEntryKey), CancellationToken.None);

        result.Type.Should().Be(ResultType.Conflict);
        result.ErrorCode.Should().Be("KurinNumberTaken");
        _provisioning.Verify(x => x.ProvisionAsync(It.IsAny<AccountProvisioningRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private WaitlistEntry Given(string? claimed, bool preparatory = false)
    {
        var entry = new WaitlistEntry
        {
            WaitlistEntryKey = Guid.NewGuid(),
            FirstName = "Olena",
            LastName = "Lviv",
            Email = "olena@example.com",
            IsKurinLeaderCandidate = true,
            ClaimedKurinNameOrNumber = claimed,
            IsPreparatoryKurin = preparatory,
            VerificationStatus = WaitlistVerificationStatus.Submitted
        };
        _waitlist.Setup(x => x.GetByKeyAsync(entry.WaitlistEntryKey, It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        return entry;
    }
}
