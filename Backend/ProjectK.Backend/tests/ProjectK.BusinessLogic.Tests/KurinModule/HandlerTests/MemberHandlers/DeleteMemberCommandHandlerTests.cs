using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Delete;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MemberHandlers
{
    public class DeleteMemberHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMemberRepository> _memberRepositoryMock;
        private readonly DeleteMemberHandler _handler;

        public DeleteMemberHandlerTests()
        {
            _memberRepositoryMock = new Mock<IMemberRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock.Setup(u => u.Members).Returns(_memberRepositoryMock.Object);

            _handler = new DeleteMemberHandler(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_WhenMemberKeyIsEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var command = new DeleteMember(Guid.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.BadRequest);
            result.Data.Should().Be("MemberKey cannot be empty.");
            _memberRepositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _memberRepositoryMock.Verify(r => r.Delete(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenMemberDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var memberKey = Guid.NewGuid();
            var command = new DeleteMember(memberKey);

            _memberRepositoryMock
                .Setup(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Member)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.NotFound);
            result.Data.Should().Be($"Member with key {memberKey} not found.");
            _memberRepositoryMock.Verify(r => r.Delete(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenMemberExistsAndDeleted_ShouldReturnSuccess()
        {
            // Arrange
            var memberKey = Guid.NewGuid();
            var member = new Member { MemberKey = memberKey, GroupKey = Guid.NewGuid(), KurinKey = Guid.NewGuid(), FirstName = "A", LastName = "B", MiddleName = "C", Email = "a@example.com", PhoneNumber = "123", DateOfBirth = new DateOnly(2000, 1, 1) };
            var command = new DeleteMember(memberKey);

            _memberRepositoryMock
                .Setup(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.Success);
            result.Data.Should().BeNull();
            _memberRepositoryMock.Verify(r => r.Delete(member, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenSaveChangesReturnsZero_ShouldReturnInternalServerError()
        {
            // Arrange
            var memberKey = Guid.NewGuid();
            var member = new Member { MemberKey = memberKey, GroupKey = Guid.NewGuid(), KurinKey = Guid.NewGuid(), FirstName = "X", LastName = "Y", MiddleName = "Z", Email = "x@example.com", PhoneNumber = "000", DateOfBirth = new DateOnly(1999, 1, 1) };
            var command = new DeleteMember(memberKey);

            _memberRepositoryMock
                .Setup(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.InternalServerError);
            result.Data.Should().Be("Failed to delete Member due to internal error.");
            _memberRepositoryMock.Verify(r => r.Delete(member, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenRepositoryDeleteThrows_ShouldPropagateException()
        {
            // Arrange
            var memberKey = Guid.NewGuid();
            var member = new Member { MemberKey = memberKey, GroupKey = Guid.NewGuid(), KurinKey = Guid.NewGuid(), FirstName = "Err", LastName = "Case", MiddleName = "M", Email = "err@example.com", PhoneNumber = "111", DateOfBirth = new DateOnly(1990, 1, 1) };
            var command = new DeleteMember(memberKey);
            var expected = new Exception("Delete failed");

            _memberRepositoryMock
                .Setup(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _memberRepositoryMock
                .Setup(r => r.Delete(member, It.IsAny<CancellationToken>()))
                .Throws(expected);

            // Act
            var ex = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));

            // Assert
            ex.Should().BeSameAs(expected);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
