using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Delete;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Enums;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MemberHandlers;

/// <summary>
/// Deleting a member is now two statements: clear what the kurin keeps pointed at them, and ask
/// the member module to remove them. What removal drags along — progress, history — is the
/// module's business and is covered where that lives.
/// </summary>
public class DeleteMemberHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMemberDirectory> _memberDirectory = new();
    private readonly Mock<IAgendaItemRepository> _agendaItemRepositoryMock = new();
    private readonly DeleteMemberHandler _handler;

    public DeleteMemberHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.AgendaItems).Returns(_agendaItemRepositoryMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _memberDirectory
            .Setup(d => d.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _memberDirectory
            .Setup(d => d.RemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new DeleteMemberHandler(_unitOfWorkMock.Object, _memberDirectory.Object);
    }

    [Fact]
    public async Task Handle_WithoutAKey_ShouldReturnBadRequest()
    {
        var result = await _handler.Handle(new DeleteMember(Guid.Empty), CancellationToken.None);

        result.Type.Should().Be(ResultType.BadRequest);
        result.ErrorCode.Should().Be("MemberKeyRequired");
    }

    [Fact]
    public async Task Handle_WhenMemberDoesNotExist_ShouldReturnNotFound()
    {
        _memberDirectory
            .Setup(d => d.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new DeleteMember(Guid.NewGuid()), CancellationToken.None);

        result.Type.Should().Be(ResultType.NotFound);
        _memberDirectory.Verify(
            d => d.RemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldClearAgendaAssignmentsBeforeRemovingThePerson()
    {
        var memberKey = Guid.NewGuid();

        var result = await _handler.Handle(new DeleteMember(memberKey), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        _agendaItemRepositoryMock.Verify(
            r => r.RemoveAssignmentsForTargetsAsync(
                It.Is<IReadOnlyCollection<Guid>>(keys => keys.Single() == memberKey),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _memberDirectory.Verify(
            d => d.RemoveAsync(memberKey, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNothingWasWritten_ShouldReturnInternalServerError()
    {
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _handler.Handle(new DeleteMember(Guid.NewGuid()), CancellationToken.None);

        result.Type.Should().Be(ResultType.InternalServerError);
        result.ErrorCode.Should().Be("MemberDeleteFailed");
    }

    [Fact]
    public async Task Handle_WhenRemovalThrows_ShouldPropagateException()
    {
        _memberDirectory
            .Setup(d => d.RemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var act = async () => await _handler.Handle(new DeleteMember(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>().WithMessage("boom");
    }
}
