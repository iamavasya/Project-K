using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.RefreshToken;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.RefreshToken.Handlers;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Enums;
using System.Security.Claims;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests.RefreshToken
{
    public class RefreshTokenCommandHandlerTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly RefreshTokenCommandHandler _handler;

        public RefreshTokenCommandHandlerTests()
        {
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);
            _jwtServiceMock = new Mock<IJwtService>();
            _handler = new RefreshTokenCommandHandler(_userManagerMock.Object, _jwtServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenValidRefreshToken()
        {
            // Arrange
            var refreshTokenValue = "valid-refresh-token";
            var command = new RefreshTokenCommand(refreshTokenValue);
            var userId = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var user = new AppUser
            {
                Id = userId,
                Email = "test@example.com",
                RefreshToken = refreshTokenValue,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
                KurinKey = kurinKey,
                FirstName = "John",
                LastName = "Doe"
            };

            var accessToken = "new-access-token";
            var newRefreshToken = new Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "new-refresh-token",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };
            var roles = new List<string> { "User" };

            SetupUserManagerUsers(new List<AppUser> { user });
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _jwtServiceMock.Setup(x => x.GenerateAccessToken(userId.ToString(), user.Email, roles, kurinKey.ToString()))
                .Returns(accessToken);
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(newRefreshToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            Assert.Equal(accessToken, result.Data.AccessToken);
            Assert.Equal(newRefreshToken, result.Data.RefreshToken);

            _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(userId.ToString(), user.Email, roles, kurinKey.ToString()), Times.Once);
            _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenValidRefreshTokenAndEmptyKurinKey()
        {
            // Arrange
            var refreshTokenValue = "valid-refresh-token";
            var command = new RefreshTokenCommand(refreshTokenValue);
            var userId = Guid.NewGuid();
            var user = new AppUser
            {
                Id = userId,
                Email = "test@example.com",
                RefreshToken = refreshTokenValue,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
                KurinKey = Guid.Empty,
                FirstName = "John",
                LastName = "Doe"
            };

            var accessToken = "new-access-token";
            var newRefreshToken = new Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "new-refresh-token",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };
            var roles = new List<string> { "Admin" };

            SetupUserManagerUsers(new List<AppUser> { user });
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _jwtServiceMock.Setup(x => x.GenerateAccessToken(userId.ToString(), user.Email, roles, null))
                .Returns(accessToken);
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(newRefreshToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            Assert.Equal(accessToken, result.Data.AccessToken);
            Assert.Equal(newRefreshToken, result.Data.RefreshToken);

            _jwtServiceMock.Verify(x => x.GenerateAccessToken(userId.ToString(), user.Email, roles, null), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenUserNotFound()
        {
            // Arrange
            var refreshTokenValue = "invalid-refresh-token";
            var command = new RefreshTokenCommand(refreshTokenValue);

            SetupUserManagerUsers(new List<AppUser>());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Unauthorized, result.Type);
            Assert.Null(result.Data);

            _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<AppUser>()), Times.Never);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()), Times.Never);
            _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenRefreshTokenExpired()
        {
            // Arrange
            var refreshTokenValue = "expired-refresh-token";
            var command = new RefreshTokenCommand(refreshTokenValue);
            var userId = Guid.NewGuid();
            var user = new AppUser
            {
                Id = userId,
                Email = "test@example.com",
                RefreshToken = refreshTokenValue,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1), // Expired
                KurinKey = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe"
            };

            SetupUserManagerUsers(new List<AppUser> { user });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Unauthorized, result.Type);
            Assert.Null(result.Data);

            _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<AppUser>()), Times.Never);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()), Times.Never);
            _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldUpdateUserRefreshTokenAndExpiryTime()
        {
            // Arrange
            var refreshTokenValue = "valid-refresh-token";
            var command = new RefreshTokenCommand(refreshTokenValue);
            var userId = Guid.NewGuid();
            var user = new AppUser
            {
                Id = userId,
                Email = "test@example.com",
                RefreshToken = refreshTokenValue,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
                KurinKey = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe"
            };

            var newRefreshToken = new Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "new-refresh-token",
                Expires = DateTime.UtcNow.AddDays(14),
                Created = DateTime.UtcNow
            };

            SetupUserManagerUsers(new List<AppUser> { user });
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _jwtServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .Returns("access-token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(newRefreshToken);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(newRefreshToken.Token, user.RefreshToken);
            Assert.Equal(newRefreshToken.Expires, user.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WithMultipleRoles()
        {
            // Arrange
            var refreshTokenValue = "valid-refresh-token";
            var command = new RefreshTokenCommand(refreshTokenValue);
            var userId = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var user = new AppUser
            {
                Id = userId,
                Email = "admin@example.com",
                RefreshToken = refreshTokenValue,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
                KurinKey = kurinKey,
                FirstName = "Admin",
                LastName = "User"
            };

            var roles = new List<string> { "Admin", "User", "Manager" };
            var accessToken = "admin-access-token";
            var newRefreshToken = new Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "admin-refresh-token",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };

            SetupUserManagerUsers(new List<AppUser> { user });
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _jwtServiceMock.Setup(x => x.GenerateAccessToken(userId.ToString(), user.Email, roles, kurinKey.ToString()))
                .Returns(accessToken);
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(newRefreshToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            Assert.Equal(accessToken, result.Data.AccessToken);
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(userId.ToString(), user.Email, roles, kurinKey.ToString()), Times.Once);
        }

        private void SetupUserManagerUsers(List<AppUser> users)
        {
            var queryable = users.AsQueryable();
            _userManagerMock.Setup(x => x.Users).Returns(queryable);
        }
    }
}
