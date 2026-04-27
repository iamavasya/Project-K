using Microsoft.AspNetCore.Identity;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;

using ProjectK.Common.Interfaces;
using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests.Login
{
    public class LoginUserCommandHandlerTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<SignInManager<AppUser>> _signInManagerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly LoginUserCommandHandler _handler;

        public LoginUserCommandHandlerTests()
        {
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            var contextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
            _signInManagerMock = new Mock<SignInManager<AppUser>>(
                _userManagerMock.Object, contextAccessorMock.Object, userPrincipalFactoryMock.Object, null, null, null, null);

            _jwtServiceMock = new Mock<IJwtService>();
            
            var memberRepoMock = new Mock<IMemberRepository>();
            memberRepoMock.Setup(r => r.GetByUserKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Member?)null);
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.Setup(u => u.Members).Returns(memberRepoMock.Object);

            _handler = new LoginUserCommandHandler(_userManagerMock.Object, _signInManagerMock.Object, _jwtServiceMock.Object, _uowMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenValidCredentials()
        {
            // Arrange
            var email = "test@example.com";
            var password = "password123";
            var command = new LoginUserCommand(email, password);
            var userId = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var memberKey = Guid.NewGuid();

            var user = new AppUser
            {
                Id = userId,
                Email = email,
                KurinKey = kurinKey,
                FirstName = "John",
                LastName = "Doe"
            };

            var roles = new List<string> { "User" };
            var accessToken = "access-token";
            var refreshToken = new ProjectK.Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "refresh-token",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _jwtServiceMock.Setup(x => x.GenerateAccessToken(userId.ToString(), email, roles, kurinKey.ToString()))
                .Returns(accessToken);
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(refreshToken);

            _uowMock.Setup(x => x.Members.GetByUserKeyAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Member { MemberKey = memberKey, UserKey = userId });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            Assert.Equal(userId, result.Data.UserKey);
            Assert.Equal(memberKey, result.Data.MemberKey);
            Assert.Equal(email, result.Data.Email);
            Assert.Equal("User", result.Data.Role);
            Assert.Equal(kurinKey.ToString(), result.Data.KurinKey);
            Assert.Equal(accessToken, result.Data.Tokens.AccessToken);
            Assert.Equal(refreshToken.Token, result.Data.Tokens.RefreshToken.Token);

            // Verify user was updated with new refresh token
            Assert.Equal(refreshToken.Token, user.RefreshToken);
            Assert.Equal(refreshToken.Expires, user.RefreshTokenExpiryTime);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenValidCredentialsAndEmptyKurinKey()
        {
            // Arrange
            var email = "admin@example.com";
            var password = "adminpassword";
            var command = new LoginUserCommand(email, password);
            var userId = Guid.NewGuid();

            var user = new AppUser
            {
                Id = userId,
                Email = email,
                KurinKey = Guid.Empty,
                FirstName = "Admin",
                LastName = "User"
            };

            var roles = new List<string> { "Admin" };
            var accessToken = "admin-access-token";
            var refreshToken = new Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "admin-refresh-token",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _jwtServiceMock.Setup(x => x.GenerateAccessToken(userId.ToString(), email, roles, null))
                .Returns(accessToken);
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(refreshToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            Assert.Equal(userId, result.Data.UserKey);
            Assert.Equal(email, result.Data.Email);
            Assert.Equal("Admin", result.Data.Role);
            Assert.Null(result.Data.KurinKey);

            _jwtServiceMock.Verify(x => x.GenerateAccessToken(userId.ToString(), email, roles, null), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenUserNotFound()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var password = "password123";
            var command = new LoginUserCommand(email, password);

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync((AppUser?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Unauthorized, result.Type);
            Assert.Null(result.Data);

            _userManagerMock.Verify(x => x.FindByEmailAsync(email), Times.Once);
            _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<AppUser>()), Times.Never);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()), Times.Never);
            _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenPasswordIsIncorrect()
        {
            // Arrange
            var email = "test@example.com";
            var password = "wrongpassword";
            var command = new LoginUserCommand(email, password);
            var userId = Guid.NewGuid();

            var user = new AppUser
            {
                Id = userId,
                Email = email,
                KurinKey = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Unauthorized, result.Type);
            Assert.Null(result.Data);

            _userManagerMock.Verify(x => x.FindByEmailAsync(email), Times.Once);
            _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(user, password, false), Times.Once);
            _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<AppUser>()), Times.Never);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()), Times.Never);
            _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenAccountIsLockedOut()
        {
            // Arrange
            var email = "lockedout@example.com";
            var password = "password123";
            var command = new LoginUserCommand(email, password);
            var userId = Guid.NewGuid();

            var user = new AppUser
            {
                Id = userId,
                Email = email,
                KurinKey = Guid.NewGuid(),
                FirstName = "Locked",
                LastName = "User"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Unauthorized, result.Type);
            Assert.Null(result.Data);

            _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(user, password, false), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenTwoFactorRequired()
        {
            // Arrange
            var email = "2fa@example.com";
            var password = "password123";
            var command = new LoginUserCommand(email, password);
            var userId = Guid.NewGuid();

            var user = new AppUser
            {
                Id = userId,
                Email = email,
                KurinKey = Guid.NewGuid(),
                FirstName = "TwoFactor",
                LastName = "User"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Unauthorized, result.Type);
            Assert.Null(result.Data);

            _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(user, password, false), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldHandleMultipleRoles()
        {
            // Arrange
            var email = "multirole@example.com";
            var password = "password123";
            var command = new LoginUserCommand(email, password);
            var userId = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();

            var user = new AppUser
            {
                Id = userId,
                Email = email,
                KurinKey = kurinKey,
                FirstName = "Multi",
                LastName = "Role"
            };

            var roles = new List<string> { "Admin", "Manager", "User" };
            var accessToken = "multi-role-token";
            var refreshToken = new Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "multi-role-refresh",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _jwtServiceMock.Setup(x => x.GenerateAccessToken(userId.ToString(), email, roles, kurinKey.ToString()))
                .Returns(accessToken);
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(refreshToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            Assert.Equal("Admin", result.Data.Role); // Should return first role
            Assert.Equal(accessToken, result.Data.Tokens.AccessToken);

            _jwtServiceMock.Verify(x => x.GenerateAccessToken(userId.ToString(), email, roles, kurinKey.ToString()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldUpdateUserRefreshTokenAndExpiryTime()
        {
            // Arrange
            var email = "update@example.com";
            var password = "password123";
            var command = new LoginUserCommand(email, password);
            var userId = Guid.NewGuid();

            var user = new AppUser
            {
                Id = userId,
                Email = email,
                KurinKey = Guid.NewGuid(),
                FirstName = "Update",
                LastName = "Test",
                RefreshToken = "old-token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1) // Old expired token
            };

            var roles = new List<string> { "User" };
            var newRefreshToken = new Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "new-refresh-token",
                Expires = DateTime.UtcNow.AddDays(14),
                Created = DateTime.UtcNow
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);
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
    }
}