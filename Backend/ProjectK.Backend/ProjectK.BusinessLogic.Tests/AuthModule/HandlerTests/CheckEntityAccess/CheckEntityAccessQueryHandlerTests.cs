using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Queries;
using ProjectK.BusinessLogic.Modules.AuthModule.Queries.Handlers;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests.CheckEntityAccess
{
    public class CheckEntityAccessQueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGroupRepository> _groupRepositoryMock;
        private readonly Mock<IMemberRepository> _memberRepositoryMock;
        private readonly CheckEntityAccessQueryHandler _handler;

        public CheckEntityAccessQueryHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _groupRepositoryMock = new Mock<IGroupRepository>();
            _memberRepositoryMock = new Mock<IMemberRepository>();

            _unitOfWorkMock.Setup(x => x.Groups).Returns(_groupRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.Members).Returns(_memberRepositoryMock.Object);

            _handler = new CheckEntityAccessQueryHandler(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnTrue_WhenGroupExistsAndKurinKeyMatches()
        {
            // Arrange
            var groupKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var query = new CheckEntityAccessQuery
            {
                EntityType = "group",
                EntityKey = groupKey.ToString(),
                ActiveKurinKey = kurinKey.ToString()
            };

            var group = new Group("Test Group", kurinKey)
            {
                GroupKey = groupKey,
                KurinKey = kurinKey
            };

            _groupRepositoryMock.Setup(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.True(result.Data);
            _groupRepositoryMock.Verify(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenGroupExistsButKurinKeyDoesNotMatch()
        {
            // Arrange
            var groupKey = Guid.NewGuid();
            var groupKurinKey = Guid.NewGuid();
            var activeKurinKey = Guid.NewGuid();
            var query = new CheckEntityAccessQuery
            {
                EntityType = "group",
                EntityKey = groupKey.ToString(),
                ActiveKurinKey = activeKurinKey.ToString()
            };

            var group = new Group("Test Group", groupKurinKey)
            {
                GroupKey = groupKey,
                KurinKey = groupKurinKey
            };

            _groupRepositoryMock.Setup(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.False(result.Data);
            _groupRepositoryMock.Verify(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenGroupDoesNotExist()
        {
            // Arrange
            var groupKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var query = new CheckEntityAccessQuery
            {
                EntityType = "group",
                EntityKey = groupKey.ToString(),
                ActiveKurinKey = kurinKey.ToString()
            };

            _groupRepositoryMock.Setup(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Group?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.False(result.Data);
            _groupRepositoryMock.Verify(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnTrue_WhenMemberExistsAndKurinKeyMatches()
        {
            // Arrange
            var memberKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var groupKey = Guid.NewGuid();
            var query = new CheckEntityAccessQuery
            {
                EntityType = "member",
                EntityKey = memberKey.ToString(),
                ActiveKurinKey = kurinKey.ToString()
            };

            var member = new Member
            {
                MemberKey = memberKey,
                KurinKey = kurinKey,
                GroupKey = groupKey,
                FirstName = "John",
                LastName = "Doe",
                MiddleName = "M",
                Email = "john.doe@example.com",
                PhoneNumber = "123456789",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };

            _memberRepositoryMock.Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.True(result.Data);
            _memberRepositoryMock.Verify(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenMemberExistsButKurinKeyDoesNotMatch()
        {
            // Arrange
            var memberKey = Guid.NewGuid();
            var memberKurinKey = Guid.NewGuid();
            var activeKurinKey = Guid.NewGuid();
            var groupKey = Guid.NewGuid();
            var query = new CheckEntityAccessQuery
            {
                EntityType = "member",
                EntityKey = memberKey.ToString(),
                ActiveKurinKey = activeKurinKey.ToString()
            };

            var member = new Member
            {
                MemberKey = memberKey,
                KurinKey = memberKurinKey,
                GroupKey = groupKey,
                FirstName = "Jane",
                LastName = "Smith",
                MiddleName = "A",
                Email = "jane.smith@example.com",
                PhoneNumber = "987654321",
                DateOfBirth = new DateOnly(1985, 5, 15)
            };

            _memberRepositoryMock.Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.False(result.Data);
            _memberRepositoryMock.Verify(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenMemberDoesNotExist()
        {
            // Arrange
            var memberKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var query = new CheckEntityAccessQuery
            {
                EntityType = "member",
                EntityKey = memberKey.ToString(),
                ActiveKurinKey = kurinKey.ToString()
            };

            _memberRepositoryMock.Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Member?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.False(result.Data);
            _memberRepositoryMock.Verify(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnBadRequest_WhenEntityTypeIsInvalid()
        {
            // Arrange
            var entityKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var query = new CheckEntityAccessQuery
            {
                EntityType = "invalid",
                EntityKey = entityKey.ToString(),
                ActiveKurinKey = kurinKey.ToString()
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.BadRequest, result.Type);
            Assert.False(result.Data);
            Assert.Equal("Invalid entity type.", result.CreatedAtActionName);

            _groupRepositoryMock.Verify(x => x.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _memberRepositoryMock.Verify(x => x.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldBeCaseInsensitive_ForEntityType()
        {
            // Arrange
            var groupKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var query = new CheckEntityAccessQuery
            {
                EntityType = "GROUP", // Uppercase
                EntityKey = groupKey.ToString(),
                ActiveKurinKey = kurinKey.ToString()
            };

            var group = new Group("Test Group", kurinKey)
            {
                GroupKey = groupKey,
                KurinKey = kurinKey
            };

            _groupRepositoryMock.Setup(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.True(result.Data);
            _groupRepositoryMock.Verify(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldHandleMixedCaseEntityType()
        {
            // Arrange
            var memberKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var groupKey = Guid.NewGuid();
            var query = new CheckEntityAccessQuery
            {
                EntityType = "MeMbEr", // Mixed case
                EntityKey = memberKey.ToString(),
                ActiveKurinKey = kurinKey.ToString()
            };

            var member = new Member
            {
                MemberKey = memberKey,
                KurinKey = kurinKey,
                GroupKey = groupKey,
                FirstName = "Test",
                LastName = "User",
                MiddleName = "T",
                Email = "test@example.com",
                PhoneNumber = "555-0123",
                DateOfBirth = new DateOnly(1995, 12, 25)
            };

            _memberRepositoryMock.Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.True(result.Data);
            _memberRepositoryMock.Verify(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldHandleInvalidGuidFormat()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var query = new CheckEntityAccessQuery
            {
                EntityType = "group",
                EntityKey = "invalid-guid-format",
                ActiveKurinKey = kurinKey.ToString()
            };

            _groupRepositoryMock.Setup(x => x.GetByKeyAsync(Guid.Empty, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Group?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.False(result.Data);
            _groupRepositoryMock.Verify(x => x.GetByKeyAsync(Guid.Empty, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenActiveKurinKeyIsNull()
        {
            // Arrange
            var groupKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var query = new CheckEntityAccessQuery
            {
                EntityType = "group",
                EntityKey = groupKey.ToString(),
                ActiveKurinKey = null
            };

            var group = new Group("Test Group", kurinKey)
            {
                GroupKey = groupKey,
                KurinKey = kurinKey
            };

            _groupRepositoryMock.Setup(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.False(result.Data);
            _groupRepositoryMock.Verify(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenActiveKurinKeyIsEmpty()
        {
            // Arrange
            var memberKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var groupKey = Guid.NewGuid();
            var query = new CheckEntityAccessQuery
            {
                EntityType = "member",
                EntityKey = memberKey.ToString(),
                ActiveKurinKey = string.Empty
            };

            var member = new Member
            {
                MemberKey = memberKey,
                KurinKey = kurinKey,
                GroupKey = groupKey,
                FirstName = "Empty",
                LastName = "Test",
                MiddleName = "E",
                Email = "empty@example.com",
                PhoneNumber = "000-0000",
                DateOfBirth = new DateOnly(2000, 1, 1)
            };

            _memberRepositoryMock.Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.False(result.Data);
            _memberRepositoryMock.Verify(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldInitializeUnitOfWorkCorrectly()
        {
            // Arrange & Act
            var handler = new CheckEntityAccessQueryHandler(_unitOfWorkMock.Object);

            // Assert
            Assert.NotNull(handler);
        }
    }
}