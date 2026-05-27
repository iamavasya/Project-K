using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding.Handlers;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests.Onboarding;

public class SubmitWaitlistRegistrationHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IWaitlistRepository> _waitlistRepository = new();
    private readonly Mock<IMemberRepository> _memberRepository = new();
    private readonly SubmitWaitlistRegistrationHandler _handler;

    public SubmitWaitlistRegistrationHandlerTests()
    {
        _unitOfWork.Setup(x => x.WaitlistEntries).Returns(_waitlistRepository.Object);
        _unitOfWork.Setup(x => x.Members).Returns(_memberRepository.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _waitlistRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaitlistEntry?)null);
        _memberRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        _handler = new SubmitWaitlistRegistrationHandler(_unitOfWork.Object);
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

    [Theory]
    [InlineData(null, "Ukraine", "StanytsiaRequired")]
    [InlineData("", "Ukraine", "StanytsiaRequired")]
    [InlineData("Kyiv", null, "RegionOrCountryRequired")]
    [InlineData("Kyiv", "", "RegionOrCountryRequired")]
    public async Task Handle_ShouldReturnBadRequest_WhenLocationFieldsAreMissing(
        string? stanytsia,
        string? regionOrCountry,
        string expectedErrorCode)
    {
        var command = CreateCommand(stanytsia, regionOrCountry);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Type.Should().Be(ResultType.BadRequest);
        result.ErrorCode.Should().Be(expectedErrorCode);
        _waitlistRepository.Verify(x => x.Create(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenKurinLeaderCandidateIsNotConfirmed()
    {
        var command = CreateCommand(isKurinLeaderCandidate: false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Type.Should().Be(ResultType.BadRequest);
        result.ErrorCode.Should().Be("KurinLeaderCandidateRequired");
        _waitlistRepository.Verify(x => x.Create(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "ClaimedKurinNumberRequired")]
    [InlineData("", "ClaimedKurinNumberRequired")]
    [InlineData("97a", "ClaimedKurinNumberInvalid")]
    [InlineData("Lisovi Chorty", "ClaimedKurinNumberInvalid")]
    public async Task Handle_ShouldReturnBadRequest_WhenKurinNumberIsMissingOrNotNumeric(
        string? claimedKurinNumber,
        string expectedErrorCode)
    {
        var command = CreateCommand(claimedKurinNumber: claimedKurinNumber);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Type.Should().Be(ResultType.BadRequest);
        result.ErrorCode.Should().Be(expectedErrorCode);
        _waitlistRepository.Verify(x => x.Create(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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
