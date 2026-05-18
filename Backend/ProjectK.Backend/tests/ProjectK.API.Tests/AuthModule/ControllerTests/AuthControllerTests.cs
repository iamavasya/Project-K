using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProjectK.API.Controllers.AuthModule;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.RefreshToken;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.AuthModule.Queries;
using ProjectK.BusinessLogic.Modules.UsersModule.Command;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Dtos.AuthModule.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System.Security.Claims;

namespace ProjectK.API.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _mapperMock = new Mock<IMapper>();

            _controller = new AuthController(_mediatorMock.Object, _mapperMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task RegisterManager_ShouldReturnOk_WhenMediatorReturnsSuccess()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "test@example.com",
                KurinNumber = 1
            };

            var serviceResult = new ServiceResult<RegisterUserResponse>(ResultType.Success, new RegisterUserResponse());
            _mediatorMock.Setup(m => m.Send(It.IsAny<RegisterManagerCommand>(), default))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.RegisterManager(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(serviceResult.Data, okResult.Value);
        }

        [Fact]
        public async Task Login_ShouldSetCookie_WhenLoginSuccessful()
        {
            // Arrange
            var request = new LoginUserRequest { Email = "test@test.com", Password = "pass" };

            var tokens = new JwtResponse
            {
                AccessToken = "AccessToken",
                RefreshToken = new RefreshToken
                {
                    Token = "RefreshToken",
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow,
                }
            };

            var serviceResult = new ServiceResult<LoginUserResponse>(ResultType.Success, new LoginUserResponse { Tokens = tokens });

            _mapperMock.Setup(m => m.Map<LoginUserCommand>(request))
                .Returns(new LoginUserCommand(request.Email, request.Password));

            _mediatorMock.Setup(m => m.Send(It.IsAny<LoginUserCommand>(), default))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.Login(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var setCookieHeader = _controller.Response.Headers["Set-Cookie"].ToString();
            Assert.Contains("refreshToken", setCookieHeader);
            Assert.Contains("samesite=lax", setCookieHeader, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secure", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Login_ShouldSetSecureSameSiteNoneCookie_WhenRequestIsHttps()
        {
            // Arrange
            _controller.Request.Scheme = "https";
            var request = new LoginUserRequest { Email = "test@test.com", Password = "pass" };
            var tokens = new JwtResponse
            {
                AccessToken = "AccessToken",
                RefreshToken = new RefreshToken
                {
                    Token = "RefreshToken",
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow,
                }
            };

            _mapperMock.Setup(m => m.Map<LoginUserCommand>(request))
                .Returns(new LoginUserCommand(request.Email, request.Password));

            _mediatorMock.Setup(m => m.Send(It.IsAny<LoginUserCommand>(), default))
                .ReturnsAsync(new ServiceResult<LoginUserResponse>(ResultType.Success, new LoginUserResponse { Tokens = tokens }));

            // Act
            var result = await _controller.Login(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var setCookieHeader = _controller.Response.Headers["Set-Cookie"].ToString();
            Assert.Contains("refreshToken", setCookieHeader);
            Assert.Contains("samesite=none", setCookieHeader, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenLoginFails()
        {
            // Arrange
            var request = new LoginUserRequest { Email = "bad@test.com", Password = "wrong" };
            var serviceResult = new ServiceResult<LoginUserResponse>(ResultType.Unauthorized);

            _mapperMock.Setup(m => m.Map<LoginUserCommand>(request))
                .Returns(new LoginUserCommand(request.Email, request.Password));

            _mediatorMock.Setup(m => m.Send(It.IsAny<LoginUserCommand>(), default))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.Login(request);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Refresh_ShouldReturnUnauthorized_WhenNoCookie()
        {
            // Arrange
            // no refreshToken cookie set

            // Act
            var result = await _controller.Refresh();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Refresh_ShouldDeleteCookieWithoutLogout_WhenRefreshTokenIsInvalid()
        {
            // Arrange
            _controller.Request.Headers.Cookie = "refreshToken=invalid-token";
            _mediatorMock.Setup(m => m.Send(It.IsAny<RefreshTokenCommand>(), default))
                .ReturnsAsync(new ServiceResult<JwtResponse>(ResultType.Unauthorized));

            // Act
            var result = await _controller.Refresh();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            Assert.Contains("refreshToken=", _controller.Response.Headers["Set-Cookie"].ToString());
            _mediatorMock.Verify(m => m.Send(It.IsAny<LogoutUserCommand>(), default), Times.Never);
        }

        [Fact]
        public async Task Refresh_ShouldUseValidToken_WhenDuplicateRefreshTokenCookiesExist()
        {
            // Arrange
            _controller.Request.Headers.Cookie = "refreshToken=stale-token; refreshToken=valid-token";
            var refreshedJwt = new JwtResponse
            {
                AccessToken = "new-access-token",
                RefreshToken = new RefreshToken
                {
                    Token = "new-refresh-token",
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow
                }
            };

            _mediatorMock.Setup(m => m.Send(
                    It.Is<RefreshTokenCommand>(c => c.RefreshToken == "stale-token"),
                    default))
                .ReturnsAsync(new ServiceResult<JwtResponse>(ResultType.Unauthorized));
            _mediatorMock.Setup(m => m.Send(
                    It.Is<RefreshTokenCommand>(c => c.RefreshToken == "valid-token"),
                    default))
                .ReturnsAsync(new ServiceResult<JwtResponse>(ResultType.Success, refreshedJwt));

            // Act
            var result = await _controller.Refresh();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(refreshedJwt, okResult.Value);
            Assert.Contains("new-refresh-token", _controller.Response.Headers["Set-Cookie"].ToString());
            _mediatorMock.Verify(m => m.Send(It.Is<RefreshTokenCommand>(c => c.RefreshToken == "stale-token"), default), Times.Once);
            _mediatorMock.Verify(m => m.Send(It.Is<RefreshTokenCommand>(c => c.RefreshToken == "valid-token"), default), Times.Once);
        }

        [Fact]
        public async Task CheckAccess_ShouldReturnOk_WhenMediatorReturnsTrue()
        {
            // Arrange
            var request = new CheckEntityAccessRequest
            {
                EntityType = "TestEntity",
                EntityKey = "123",
                ActiveKurinKey = Guid.NewGuid().ToString()
            };

            var serviceResult = new ServiceResult<bool>(ResultType.Success, true);

            _mediatorMock.Setup(m => m.Send(It.IsAny<CheckEntityAccessQuery>(), default))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.CheckAccess(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var boolValue = (bool)okResult.Value!;
            Assert.True(boolValue);

            _mediatorMock.Verify(m => m.Send(
                It.Is<CheckEntityAccessQuery>(q =>
                    q.EntityType == request.EntityType
                    && q.EntityKey == request.EntityKey
                    && q.ActiveKurinKey == null),
                default), Times.Once);
        }

        [Fact]
        public async Task GetMfaSetup_ShouldSendQueryForCurrentUser()
        {
            // Arrange
            var userKey = Guid.NewGuid();
            SetCurrentUser(userKey);
            var setup = new MfaSetupResponseDto("shared-key", "otpauth://totp/Project-K:user@example.com", "qr-base64");

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetMfaSetupQuery>(), default))
                .ReturnsAsync(new ServiceResult<MfaSetupResponseDto>(ResultType.Success, setup));

            // Act
            var result = await _controller.GetMfaSetup();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(setup, okResult.Value);
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetMfaSetupQuery>(q => q.UserKey == userKey),
                default), Times.Once);
        }

        [Fact]
        public async Task EnableMfa_ShouldSendCommandForCurrentUser()
        {
            // Arrange
            var userKey = Guid.NewGuid();
            SetCurrentUser(userKey);
            var request = new MfaVerifyRequestDto("123 456");
            var response = new MfaEnableResponseDto(true, new[] { "code-1", "code-2" });

            _mediatorMock.Setup(m => m.Send(It.IsAny<EnableMfaCommand>(), default))
                .ReturnsAsync(new ServiceResult<MfaEnableResponseDto>(ResultType.Success, response));

            // Act
            var result = await _controller.EnableMfa(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, okResult.Value);
            _mediatorMock.Verify(m => m.Send(
                It.Is<EnableMfaCommand>(cmd => cmd.UserKey == userKey && cmd.Code == request.Code),
                default), Times.Once);
        }

        [Fact]
        public async Task RotateMfaRecoveryCodes_ShouldSendCommandForCurrentUser()
        {
            // Arrange
            var userKey = Guid.NewGuid();
            SetCurrentUser(userKey);
            var request = new MfaRecoveryCodesRequestDto("current-password");
            var response = new MfaRecoveryCodesResponseDto(new[] { "code-1" });

            _mediatorMock.Setup(m => m.Send(It.IsAny<GenerateMfaRecoveryCodesCommand>(), default))
                .ReturnsAsync(new ServiceResult<MfaRecoveryCodesResponseDto>(ResultType.Success, response));

            // Act
            var result = await _controller.RotateMfaRecoveryCodes(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, okResult.Value);
            _mediatorMock.Verify(m => m.Send(
                It.Is<GenerateMfaRecoveryCodesCommand>(cmd =>
                    cmd.UserKey == userKey && cmd.CurrentPassword == request.CurrentPassword),
                default), Times.Once);
        }

        [Fact]
        public async Task VerifyMfaLogin_ShouldSetCookie_WhenVerificationReturnsTokens()
        {
            // Arrange
            var request = new MfaLoginRequestDto("user@example.com", "123456", true);
            var tokens = new JwtResponse
            {
                AccessToken = "AccessToken",
                RefreshToken = new RefreshToken
                {
                    Token = "MfaRefreshToken",
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow
                }
            };
            var response = new LoginUserResponse { Email = request.Email, Tokens = tokens };

            _mediatorMock.Setup(m => m.Send(It.IsAny<VerifyMfaLoginCommand>(), default))
                .ReturnsAsync(new ServiceResult<LoginUserResponse>(ResultType.Success, response));

            // Act
            var result = await _controller.VerifyMfaLogin(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Contains("refreshToken=MfaRefreshToken", _controller.Response.Headers["Set-Cookie"].ToString());
            _mediatorMock.Verify(m => m.Send(
                It.Is<VerifyMfaLoginCommand>(cmd =>
                    cmd.Email == request.Email &&
                    cmd.Code == request.Code &&
                    cmd.RememberMe == request.RememberMe),
                default), Times.Once);
        }

        [Fact]
        public async Task VerifyMfaLogin_ShouldNotSetCookie_WhenMfaIsUnauthorized()
        {
            // Arrange
            var request = new MfaLoginRequestDto("user@example.com", "bad-code", false);

            _mediatorMock.Setup(m => m.Send(It.IsAny<VerifyMfaLoginCommand>(), default))
                .ReturnsAsync(new ServiceResult<LoginUserResponse>(ResultType.Unauthorized));

            // Act
            var result = await _controller.VerifyMfaLogin(request);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            Assert.DoesNotContain("refreshToken", _controller.Response.Headers["Set-Cookie"].ToString());
        }

        private void SetCurrentUser(Guid userKey)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userKey.ToString()) };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims))
                }
            };
        }
    }
}
