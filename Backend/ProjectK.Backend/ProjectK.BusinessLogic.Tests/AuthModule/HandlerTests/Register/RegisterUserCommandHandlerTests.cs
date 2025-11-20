using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests.Register
{
    public class RegisterUserCommandHandlerTests
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<RoleManager<AppRole>> _roleManagerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly RegisterUserCommandHandler _handler;

        public RegisterUserCommandHandlerTests()
        {
            _mapperMock = new Mock<IMapper>();

            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            var roleStoreMock = new Mock<IRoleStore<AppRole>>();
            _roleManagerMock = new Mock<RoleManager<AppRole>>(
                roleStoreMock.Object, null, null, null, null);

            _jwtServiceMock = new Mock<IJwtService>();

            _handler = new RegisterUserCommandHandler(
                _mapperMock.Object,
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _jwtServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenValidRegistrationWithKurinKey()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe",
                Role = "User",
                KurinKey = kurinKey
            };

            var userId = Guid.NewGuid();
            var user = new AppUser
            {
                Id = userId,
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                KurinKey = kurinKey
            };

            var roles = new List<string> { "User" };
            var accessToken = "access-token";
            var refreshToken = new Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "refresh-token",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };

            _mapperMock.Setup(x => x.Map<AppUser>(command))
                .Returns(user);
            _userManagerMock.Setup(x => x.CreateAsync(user, command.Password))
                .ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(x => x.RoleExistsAsync(command.Role))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.AddToRoleAsync(user, command.Role))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _jwtServiceMock.Setup(x => x.GenerateAccessToken(userId.ToString(), command.Email, roles, kurinKey.ToString()))
                .Returns(accessToken);
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(refreshToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            Assert.Equal(userId, result.Data.UserId);
            Assert.Equal(command.Email, result.Data.Email);
            Assert.Equal(command.FirstName, result.Data.FirstName);
            Assert.Equal(command.LastName, result.Data.LastName);
            Assert.Equal(accessToken, result.Data.Tokens.AccessToken);
            Assert.Equal(refreshToken, result.Data.Tokens.RefreshToken);

            // Verify user was updated with refresh token
            Assert.Equal(command.Email, user.UserName);
            Assert.Equal(refreshToken.Token, user.RefreshToken);
            Assert.Equal(refreshToken.Expires, user.RefreshTokenExpiryTime);

            // Verify all calls
            _mapperMock.Verify(x => x.Map<AppUser>(command), Times.Once);
            _userManagerMock.Verify(x => x.CreateAsync(user, command.Password), Times.Once);
            _roleManagerMock.Verify(x => x.RoleExistsAsync(command.Role), Times.Once);
            _userManagerMock.Verify(x => x.AddToRoleAsync(user, command.Role), Times.Once);
            _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(userId.ToString(), command.Email, roles, kurinKey.ToString()), Times.Once);
            _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenValidRegistrationWithEmptyKurinKey()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "admin@example.com",
                Password = "AdminPassword123!",
                FirstName = "Admin",
                LastName = "User",
                Role = "Admin",
                KurinKey = Guid.Empty
            };

            var userId = Guid.NewGuid();
            var user = new AppUser
            {
                Id = userId,
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                KurinKey = Guid.Empty
            };

            var roles = new List<string> { "Admin" };
            var accessToken = "admin-access-token";
            var refreshToken = new Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "admin-refresh-token",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };

            SetupSuccessfulRegistration(command, user, roles, accessToken, refreshToken);
            _jwtServiceMock.Setup(x => x.GenerateAccessToken(userId.ToString(), command.Email, roles, null))
                .Returns(accessToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(userId.ToString(), command.Email, roles, null), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentNullException_WhenDefaultUserValidRegistrationWithNullKurinKey()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "user@example.com",
                Password = "userPassword123!",
                FirstName = "Casual",
                LastName = "User",
                Role = "User",
                KurinKey = null
            };

            var userId = Guid.NewGuid();
            var user = new AppUser
            {
                Id = userId,
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                KurinKey = null
            };

            var roles = new List<string> { "User" };
            var accessToken = "user-access-token";
            var refreshToken = new Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "user-refresh-token",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };

            SetupSuccessfulRegistration(command, user, roles, accessToken, refreshToken);
            _jwtServiceMock.Setup(x => x.GenerateAccessToken(userId.ToString(), command.Email, roles, null))
                .Returns(accessToken);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            // Verify that mapping was called but no further operations were performed
            _mapperMock.Verify(x => x.Map<AppUser>(command), Times.Once);
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldCreateRole_WhenRoleDoesNotExist()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "newrole@example.com",
                Password = "Password123!",
                FirstName = "New",
                LastName = "Role",
                Role = "NewRole",
                KurinKey = Guid.NewGuid()
            };

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName
            };

            var roles = new List<string> { "NewRole" };

            _mapperMock.Setup(x => x.Map<AppUser>(command))
                .Returns(user);
            _userManagerMock.Setup(x => x.CreateAsync(user, command.Password))
                .ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(x => x.RoleExistsAsync(command.Role))
                .ReturnsAsync(false);
            _roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<AppRole>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(user, command.Role))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _jwtServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .Returns("access-token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(new Common.Models.Dtos.AuthModule.RefreshToken { Token = "refresh-token", Expires = DateTime.UtcNow.AddDays(7) });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            _roleManagerMock.Verify(x => x.RoleExistsAsync(command.Role), Times.Once);
            _roleManagerMock.Verify(x => x.CreateAsync(It.Is<AppRole>(r => r.Name == command.Role)), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldNotCreateRole_WhenRoleAlreadyExists()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "existing@example.com",
                Password = "Password123!",
                FirstName = "Existing",
                LastName = "Role",
                Role = "User",
                KurinKey = Guid.NewGuid()
            };

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName
            };

            var roles = new List<string> { "User" };

            SetupSuccessfulRegistration(command, user, roles, "access-token",
                new Common.Models.Dtos.AuthModule.RefreshToken { Token = "refresh-token", Expires = DateTime.UtcNow.AddDays(7) });
            _roleManagerMock.Setup(x => x.RoleExistsAsync(command.Role))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            _roleManagerMock.Verify(x => x.RoleExistsAsync(command.Role), Times.Once);
            _roleManagerMock.Verify(x => x.CreateAsync(It.IsAny<AppRole>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowApplicationException_WhenUserCreationFails()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "invalid@example.com",
                Password = "weak",
                FirstName = "Invalid",
                LastName = "User",
                Role = "User",
                KurinKey = Guid.NewGuid()
            };

            var user = new AppUser
            {
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName
            };

            var identityErrors = new[]
            {
                new IdentityError { Description = "Password too short" },
                new IdentityError { Description = "Email already exists" }
            };

            _mapperMock.Setup(x => x.Map<AppUser>(command))
                .Returns(user);
            _userManagerMock.Setup(x => x.CreateAsync(user, command.Password))
                .ReturnsAsync(IdentityResult.Failed(identityErrors));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, CancellationToken.None));

            Assert.Contains("User registration failed", exception.Message);
            Assert.Contains("Password too short", exception.Message);
            Assert.Contains("Email already exists", exception.Message);

            _mapperMock.Verify(x => x.Map<AppUser>(command), Times.Once);
            _userManagerMock.Verify(x => x.CreateAsync(user, command.Password), Times.Once);
            _roleManagerMock.Verify(x => x.RoleExistsAsync(It.IsAny<string>()), Times.Never);
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldSetUserNameToEmail()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "username@example.com",
                Password = "Password123!",
                FirstName = "Username",
                LastName = "Test",
                Role = "User",
                KurinKey = Guid.NewGuid()
            };

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName
            };

            var roles = new List<string> { "User" };

            SetupSuccessfulRegistration(command, user, roles, "access-token",
                new Common.Models.Dtos.AuthModule.RefreshToken { Token = "refresh-token", Expires = DateTime.UtcNow.AddDays(7) });

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(command.Email, user.UserName);
        }

        [Fact]
        public async Task Handle_ShouldHandleMultipleRoles()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "multirole@example.com",
                Password = "Password123!",
                FirstName = "Multi",
                LastName = "Role",
                Role = "Admin"
            };

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName
            };

            var roles = new List<string> { "Admin", "Manager" }; // User might have multiple roles after creation

            SetupSuccessfulRegistration(command, user, roles, "multi-access-token",
                new Common.Models.Dtos.AuthModule.RefreshToken { Token = "multi-refresh-token", Expires = DateTime.UtcNow.AddDays(7) });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(
                user.Id.ToString(),
                command.Email,
                roles,
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldUpdateUserWithRefreshTokenAndExpiryTime()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "refresh@example.com",
                Password = "Password123!",
                FirstName = "Refresh",
                LastName = "Test",
                Role = "User",
                KurinKey = Guid.NewGuid()
            };

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName
            };

            var roles = new List<string> { "User" };
            var refreshToken = new Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "new-refresh-token",
                Expires = DateTime.UtcNow.AddDays(14),
                Created = DateTime.UtcNow
            };

            SetupSuccessfulRegistration(command, user, roles, "access-token", refreshToken);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(refreshToken.Token, user.RefreshToken);
            Assert.Equal(refreshToken.Expires, user.RefreshTokenExpiryTime);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnCorrectResponseStructure()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "structure@example.com",
                Password = "Password123!",
                FirstName = "Structure",
                LastName = "Test",
                Role = "User",
                KurinKey = Guid.NewGuid()
            };

            var userId = Guid.NewGuid();
            var user = new AppUser
            {
                Id = userId,
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                KurinKey = command.KurinKey
            };

            var roles = new List<string> { "User" };
            var accessToken = "structure-access-token";
            var refreshToken = new Common.Models.Dtos.AuthModule.RefreshToken
            {
                Token = "structure-refresh-token",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };

            SetupSuccessfulRegistration(command, user, roles, accessToken, refreshToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            Assert.IsType<RegisterUserResponse>(result.Data);
            Assert.Equal(userId, result.Data.UserId);
            Assert.Equal(command.Email, result.Data.Email);
            Assert.Equal(command.FirstName, result.Data.FirstName);
            Assert.Equal(command.LastName, result.Data.LastName);
            Assert.NotNull(result.Data.Tokens);
            Assert.Equal(accessToken, result.Data.Tokens.AccessToken);
            Assert.Equal(refreshToken, result.Data.Tokens.RefreshToken);
        }

        [Fact]
        public void Constructor_ShouldInitializeAllDependencies()
        {
            // Arrange & Act
            var handler = new RegisterUserCommandHandler(
                _mapperMock.Object,
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _jwtServiceMock.Object);

            // Assert
            Assert.NotNull(handler);
        }

        private void SetupSuccessfulRegistration(RegisterUserCommand command, AppUser user, List<string> roles, string accessToken, Common.Models.Dtos.AuthModule.RefreshToken refreshToken)
        {
            _mapperMock.Setup(x => x.Map<AppUser>(command))
                .Returns(user);
            _userManagerMock.Setup(x => x.CreateAsync(user, command.Password))
                .ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(x => x.RoleExistsAsync(command.Role))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.AddToRoleAsync(user, command.Role))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _jwtServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .Returns(accessToken);
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(refreshToken);
        }
    }
}