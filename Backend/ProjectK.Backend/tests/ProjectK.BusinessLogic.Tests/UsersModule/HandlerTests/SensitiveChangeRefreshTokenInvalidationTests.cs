using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers;
using ProjectK.BusinessLogic.Modules.UsersModule.Command;
using ProjectK.BusinessLogic.Modules.UsersModule.Command.Handlers;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.UsersModule.HandlerTests
{
    public class SensitiveChangeRefreshTokenInvalidationTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<IActivityLogger> _activityLoggerMock;

        public SensitiveChangeRefreshTokenInvalidationTests()
        {
            _userManagerMock = CreateUserManagerMock();
            _activityLoggerMock = new Mock<IActivityLogger>();
        }

        [Fact]
        public async Task ChangeOwnPassword_ShouldRevokeRefreshToken_WhenPasswordChangeSucceeds()
        {
            // Arrange
            var user = CreateUserWithRefreshToken();
            var handler = new ChangeOwnPasswordCommandHandler(
                _userManagerMock.Object,
                new Mock<ILogger<ChangeOwnPasswordCommandHandler>>().Object,
                _activityLoggerMock.Object);

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ChangePasswordAsync(user, "old-password", "new-password"))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await handler.Handle(new ChangeOwnPasswordCommand(user.Id, "old-password", "new-password"), CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.True(result.Data);
            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task ChangeOwnPassword_ShouldKeepRefreshToken_WhenPasswordChangeFails()
        {
            // Arrange
            var user = CreateUserWithRefreshToken();
            var handler = new ChangeOwnPasswordCommandHandler(
                _userManagerMock.Object,
                new Mock<ILogger<ChangeOwnPasswordCommandHandler>>().Object,
                _activityLoggerMock.Object);

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ChangePasswordAsync(user, "wrong-password", "new-password"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch" }));

            // Act
            var result = await handler.Handle(new ChangeOwnPasswordCommand(user.Id, "wrong-password", "new-password"), CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.BadRequest, result.Type);
            Assert.False(result.Data);
            Assert.Equal("existing-refresh-token", user.RefreshToken);
            Assert.NotNull(user.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task ResetOwnMfa_ShouldRevokeRefreshToken_WhenResetSucceeds()
        {
            // Arrange
            var user = CreateUserWithRefreshToken();
            var handler = new ResetOwnMfaCommandHandler(
                _userManagerMock.Object,
                new Mock<ILogger<ResetOwnMfaCommandHandler>>().Object,
                _activityLoggerMock.Object);

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "current-password"))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.SetTwoFactorEnabledAsync(user, false))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.ResetAuthenticatorKeyAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await handler.Handle(new ResetOwnMfaCommand(user.Id, "current-password"), CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.True(result.Data);
            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task ResetOwnMfa_ShouldReturnUnauthorizedAndKeepRefreshToken_WhenCurrentPasswordIsWrong()
        {
            // Arrange
            var user = CreateUserWithRefreshToken();
            var handler = new ResetOwnMfaCommandHandler(
                _userManagerMock.Object,
                new Mock<ILogger<ResetOwnMfaCommandHandler>>().Object,
                _activityLoggerMock.Object);

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrong-password"))
                .ReturnsAsync(false);

            // Act
            var result = await handler.Handle(new ResetOwnMfaCommand(user.Id, "wrong-password"), CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Unauthorized, result.Type);
            Assert.False(result.Data);
            Assert.Equal("existing-refresh-token", user.RefreshToken);
            Assert.NotNull(user.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.SetTwoFactorEnabledAsync(It.IsAny<AppUser>(), It.IsAny<bool>()), Times.Never);
            _userManagerMock.Verify(x => x.ResetAuthenticatorKeyAsync(It.IsAny<AppUser>()), Times.Never);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task EnableMfa_ShouldRevokeRefreshToken_WhenVerificationSucceeds()
        {
            // Arrange
            var user = CreateUserWithRefreshToken();
            var handler = new EnableMfaCommandHandler(
                _userManagerMock.Object,
                new Mock<ILogger<EnableMfaCommandHandler>>().Object,
                _activityLoggerMock.Object);
            var provider = _userManagerMock.Object.Options.Tokens.AuthenticatorTokenProvider;

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.VerifyTwoFactorTokenAsync(user, provider, "123456"))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.SetTwoFactorEnabledAsync(user, true))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))
                .ReturnsAsync(new[] { "code-1", "code-2" });

            // Act
            var result = await handler.Handle(new EnableMfaCommand(user.Id, "123-456"), CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.True(result.Data?.Enabled);
            Assert.Equal(new[] { "code-1", "code-2" }, result.Data.RecoveryCodes);
            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task EnableMfa_ShouldKeepRefreshToken_WhenVerificationFails()
        {
            // Arrange
            var user = CreateUserWithRefreshToken();
            var handler = new EnableMfaCommandHandler(
                _userManagerMock.Object,
                new Mock<ILogger<EnableMfaCommandHandler>>().Object,
                _activityLoggerMock.Object);
            var provider = _userManagerMock.Object.Options.Tokens.AuthenticatorTokenProvider;

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.VerifyTwoFactorTokenAsync(user, provider, "000000"))
                .ReturnsAsync(false);

            // Act
            var result = await handler.Handle(new EnableMfaCommand(user.Id, "000000"), CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.BadRequest, result.Type);
            Assert.Null(result.Data);
            Assert.Equal("existing-refresh-token", user.RefreshToken);
            Assert.NotNull(user.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.SetTwoFactorEnabledAsync(It.IsAny<AppUser>(), It.IsAny<bool>()), Times.Never);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task DisableOwnMfa_ShouldRevokeRefreshToken_WhenUserIsNotPrivileged()
        {
            // Arrange
            var user = CreateUserWithRefreshToken();
            user.TwoFactorEnabled = true;
            var handler = new DisableOwnMfaCommandHandler(
                _userManagerMock.Object,
                new Mock<ILogger<DisableOwnMfaCommandHandler>>().Object,
                _activityLoggerMock.Object);

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "current-password"))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new[] { UserRole.User.ToString() });
            _userManagerMock.Setup(x => x.SetTwoFactorEnabledAsync(user, false))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await handler.Handle(new DisableOwnMfaCommand(user.Id, "current-password"), CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.True(result.Data);
            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.SetTwoFactorEnabledAsync(user, false), Times.Once);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Theory]
        [InlineData(UserRole.Admin)]
        [InlineData(UserRole.Manager)]
        public async Task DisableOwnMfa_ShouldReturnForbiddenAndKeepMfa_WhenUserIsPrivileged(UserRole role)
        {
            // Arrange
            var user = CreateUserWithRefreshToken();
            user.TwoFactorEnabled = true;
            var handler = new DisableOwnMfaCommandHandler(
                _userManagerMock.Object,
                new Mock<ILogger<DisableOwnMfaCommandHandler>>().Object,
                _activityLoggerMock.Object);

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "current-password"))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new[] { role.ToString() });

            // Act
            var result = await handler.Handle(new DisableOwnMfaCommand(user.Id, "current-password"), CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Forbidden, result.Type);
            Assert.False(result.Data);
            Assert.Equal("existing-refresh-token", user.RefreshToken);
            Assert.NotNull(user.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.SetTwoFactorEnabledAsync(It.IsAny<AppUser>(), It.IsAny<bool>()), Times.Never);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task ResetUserMfa_ShouldResetTargetAndRevokeRefreshToken_WhenManagerOwnsTargetKurin()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var targetUser = CreateUserWithRefreshToken();
            targetUser.KurinKey = kurinKey;
            targetUser.TwoFactorEnabled = true;
            var currentUserContextMock = new Mock<ICurrentUserContext>();
            currentUserContextMock.Setup(x => x.IsInRole(UserRole.Admin.ToString())).Returns(false);
            currentUserContextMock.Setup(x => x.IsInRole(UserRole.Manager.ToString())).Returns(true);
            currentUserContextMock.Setup(x => x.KurinKey).Returns(kurinKey);
            var handler = new ResetUserMfaCommandHandler(
                _userManagerMock.Object,
                currentUserContextMock.Object,
                new Mock<ILogger<ResetUserMfaCommandHandler>>().Object,
                _activityLoggerMock.Object);

            _userManagerMock.Setup(x => x.FindByIdAsync(targetUser.Id.ToString())).ReturnsAsync(targetUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(targetUser))
                .ReturnsAsync(new[] { UserRole.User.ToString() });
            _userManagerMock.Setup(x => x.SetTwoFactorEnabledAsync(targetUser, false))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.ResetAuthenticatorKeyAsync(targetUser))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.UpdateAsync(targetUser)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await handler.Handle(new ResetUserMfaCommand(targetUser.Id), CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.True(result.Data);
            Assert.Null(targetUser.RefreshToken);
            Assert.Null(targetUser.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.ResetAuthenticatorKeyAsync(targetUser), Times.Once);
            _userManagerMock.Verify(x => x.UpdateAsync(targetUser), Times.Once);
        }

        [Fact]
        public async Task ResetUserMfa_ShouldReturnForbidden_WhenManagerTargetsPrivilegedUser()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var targetUser = CreateUserWithRefreshToken();
            targetUser.KurinKey = kurinKey;
            var currentUserContextMock = new Mock<ICurrentUserContext>();
            currentUserContextMock.Setup(x => x.IsInRole(UserRole.Admin.ToString())).Returns(false);
            currentUserContextMock.Setup(x => x.IsInRole(UserRole.Manager.ToString())).Returns(true);
            currentUserContextMock.Setup(x => x.KurinKey).Returns(kurinKey);
            var handler = new ResetUserMfaCommandHandler(
                _userManagerMock.Object,
                currentUserContextMock.Object,
                new Mock<ILogger<ResetUserMfaCommandHandler>>().Object,
                _activityLoggerMock.Object);

            _userManagerMock.Setup(x => x.FindByIdAsync(targetUser.Id.ToString())).ReturnsAsync(targetUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(targetUser))
                .ReturnsAsync(new[] { UserRole.Manager.ToString() });

            // Act
            var result = await handler.Handle(new ResetUserMfaCommand(targetUser.Id), CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Forbidden, result.Type);
            Assert.False(result.Data);
            _userManagerMock.Verify(x => x.ResetAuthenticatorKeyAsync(It.IsAny<AppUser>()), Times.Never);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        private static AppUser CreateUserWithRefreshToken()
        {
            return new AppUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                UserName = "user@example.com",
                FirstName = "John",
                LastName = "Doe",
                RefreshToken = "existing-refresh-token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };
        }

        private static Mock<UserManager<AppUser>> CreateUserManagerMock()
        {
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(
                userStoreMock.Object,
                Options.Create(new IdentityOptions()),
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }
    }
}
