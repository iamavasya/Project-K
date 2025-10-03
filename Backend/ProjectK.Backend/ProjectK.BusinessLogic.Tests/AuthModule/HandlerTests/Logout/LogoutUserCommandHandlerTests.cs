using Microsoft.AspNetCore.Identity;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests.Logout
{
    public class LogoutUserCommandHandlerTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly LogoutUserCommandHandler _handler;

        public LogoutUserCommandHandlerTests()
        {
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);
            _handler = new LogoutUserCommandHandler(_userManagerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenValidUserKey()
        {
            // Arrange
            var userKey = Guid.NewGuid().ToString();
            var command = new LogoutUserCommand(userKey);
            var user = new AppUser
            {
                Id = Guid.Parse(userKey),
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                RefreshToken = "existing-refresh-token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userKey))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.Equal("User logged out successfully.", result.Data);
            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiryTime);

            _userManagerMock.Verify(x => x.FindByIdAsync(userKey), Times.Once);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenUserKeyIsNull()
        {
            // Arrange
            var command = new LogoutUserCommand(null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Unauthorized, result.Type);
            Assert.Equal("Access token is missing or invalid.", result.Data);

            _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenUserKeyIsEmpty()
        {
            // Arrange
            var command = new LogoutUserCommand(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Unauthorized, result.Type);
            Assert.Equal("Access token is missing or invalid.", result.Data);

            _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenUserNotFound()
        {
            // Arrange
            var userKey = Guid.NewGuid().ToString();
            var command = new LogoutUserCommand(userKey);

            _userManagerMock.Setup(x => x.FindByIdAsync(userKey))
                .ReturnsAsync((AppUser?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.NotFound, result.Type);
            Assert.Equal("User not found.", result.Data);

            _userManagerMock.Verify(x => x.FindByIdAsync(userKey), Times.Once);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldClearRefreshTokenAndExpiryTime_WhenUserHasActiveToken()
        {
            // Arrange
            var userKey = Guid.NewGuid().ToString();
            var command = new LogoutUserCommand(userKey);
            var user = new AppUser
            {
                Id = Guid.Parse(userKey),
                Email = "active@example.com",
                FirstName = "Active",
                LastName = "User",
                RefreshToken = "active-refresh-token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30)
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userKey))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldSucceed_WhenUserAlreadyHasNullRefreshToken()
        {
            // Arrange
            var userKey = Guid.NewGuid().ToString();
            var command = new LogoutUserCommand(userKey);
            var user = new AppUser
            {
                Id = Guid.Parse(userKey),
                Email = "already@example.com",
                FirstName = "Already",
                LastName = "LoggedOut",
                RefreshToken = null,
                RefreshTokenExpiryTime = null
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userKey))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.Equal("User logged out successfully.", result.Data);
            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiryTime);

            _userManagerMock.Verify(x => x.FindByIdAsync(userKey), Times.Once);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldSucceed_WhenUserHasExpiredRefreshToken()
        {
            // Arrange
            var userKey = Guid.NewGuid().ToString();
            var command = new LogoutUserCommand(userKey);
            var user = new AppUser
            {
                Id = Guid.Parse(userKey),
                Email = "expired@example.com",
                FirstName = "Expired",
                LastName = "Token",
                RefreshToken = "expired-refresh-token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1) // Expired yesterday
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userKey))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldHandleInvalidGuidUserKey()
        {
            // Arrange
            var invalidUserKey = "invalid-guid-format";
            var command = new LogoutUserCommand(invalidUserKey);

            _userManagerMock.Setup(x => x.FindByIdAsync(invalidUserKey))
                .ReturnsAsync((AppUser?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.NotFound, result.Type);
            Assert.Equal("User not found.", result.Data);

            _userManagerMock.Verify(x => x.FindByIdAsync(invalidUserKey), Times.Once);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldCallUpdateAsyncOnce_WhenSuccessful()
        {
            // Arrange
            var userKey = Guid.NewGuid().ToString();
            var command = new LogoutUserCommand(userKey);
            var user = new AppUser
            {
                Id = Guid.Parse(userKey),
                Email = "update@example.com",
                FirstName = "Update",
                LastName = "Test",
                RefreshToken = "some-token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddHours(1)
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userKey))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldInitializeUserManagerCorrectly()
        {
            // Arrange & Act
            var handler = new LogoutUserCommandHandler(_userManagerMock.Object);

            // Assert
            Assert.NotNull(handler);
        }
    }
}