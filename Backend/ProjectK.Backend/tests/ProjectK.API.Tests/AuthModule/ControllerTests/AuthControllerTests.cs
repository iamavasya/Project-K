using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProjectK.API.Controllers.AuthModule;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.AuthModule.Queries;
using ProjectK.BusinessLogic.Modules.UsersModule.Command;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Dtos.AuthModule.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

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
    }
}
