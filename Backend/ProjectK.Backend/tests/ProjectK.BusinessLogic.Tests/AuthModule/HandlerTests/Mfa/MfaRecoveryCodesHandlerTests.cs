using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests.Mfa
{
    public class MfaRecoveryCodesHandlerTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;

        public MfaRecoveryCodesHandlerTests()
        {
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task VerifyMfaLogin_ShouldRedeemRecoveryCode_WhenTotpIsInvalid()
        {
            // Arrange
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                FirstName = "John",
                LastName = "Doe"
            };
            var response = new LoginUserResponse { UserKey = user.Id, Email = user.Email };
            var loginResponseFactoryMock = new Mock<ILoginResponseFactory>();
            var provider = _userManagerMock.Object.Options.Tokens.AuthenticatorTokenProvider;

            _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.VerifyTwoFactorTokenAsync(user, provider, "recoverycode"))
                .ReturnsAsync(false);
            _userManagerMock.Setup(x => x.RedeemTwoFactorRecoveryCodeAsync(user, "recovery-code"))
                .ReturnsAsync(IdentityResult.Success);
            loginResponseFactoryMock.Setup(x => x.CreateAsync(user, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var handler = new VerifyMfaLoginCommandHandler(
                _userManagerMock.Object,
                loginResponseFactoryMock.Object,
                new Mock<ILogger<VerifyMfaLoginCommandHandler>>().Object,
                new Mock<IActivityLogger>().Object);

            // Act
            var result = await handler.Handle(new VerifyMfaLoginCommand(user.Email, "recovery-code", true), CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.Same(response, result.Data);
            _userManagerMock.Verify(x => x.RedeemTwoFactorRecoveryCodeAsync(user, "recovery-code"), Times.Once);
        }

        [Fact]
        public async Task GenerateMfaRecoveryCodes_ShouldRequireCurrentPassword()
        {
            // Arrange
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                FirstName = "John",
                LastName = "Doe",
                TwoFactorEnabled = true
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrong-password")).ReturnsAsync(false);

            var handler = new GenerateMfaRecoveryCodesCommandHandler(
                _userManagerMock.Object,
                new Mock<ILogger<GenerateMfaRecoveryCodesCommandHandler>>().Object,
                new Mock<IActivityLogger>().Object);

            // Act
            var result = await handler.Handle(
                new GenerateMfaRecoveryCodesCommand(user.Id, "wrong-password"),
                CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Unauthorized, result.Type);
            _userManagerMock.Verify(x => x.GenerateNewTwoFactorRecoveryCodesAsync(It.IsAny<AppUser>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GenerateMfaRecoveryCodes_ShouldRotateCodes_WhenPasswordIsValid()
        {
            // Arrange
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                FirstName = "John",
                LastName = "Doe",
                TwoFactorEnabled = true
            };
            var codes = new[] { "code-1", "code-2" };

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "current-password")).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)).ReturnsAsync(codes);

            var handler = new GenerateMfaRecoveryCodesCommandHandler(
                _userManagerMock.Object,
                new Mock<ILogger<GenerateMfaRecoveryCodesCommandHandler>>().Object,
                new Mock<IActivityLogger>().Object);

            // Act
            var result = await handler.Handle(
                new GenerateMfaRecoveryCodesCommand(user.Id, "current-password"),
                CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.Equal(codes, result.Data?.RecoveryCodes);
        }
    }
}
