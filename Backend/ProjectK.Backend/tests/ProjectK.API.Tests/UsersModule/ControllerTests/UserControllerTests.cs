using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProjectK.API.Controllers.UsersModule;
using ProjectK.BusinessLogic.Modules.UsersModule.Models;
using ProjectK.BusinessLogic.Modules.UsersModule.Queries;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.API.Tests.UsersModule.ControllerTests
{
    public class UserControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new UserController(_mediatorMock.Object);
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto
                {
                    UserId = Guid.NewGuid(),
                    KurinKey = Guid.NewGuid(),
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@example.com",
                    Role = "User"
                },
                new UserDto
                {
                    UserId = Guid.NewGuid(),
                    KurinKey = Guid.NewGuid(),
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@example.com",
                    Role = "Admin"
                }
            };
            var serviceResult = new ServiceResult<IEnumerable<UserDto>>(ResultType.Success, users);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<UserDto>>(okResult.Value);
            Assert.Equal(users.Count, returnedUsers.Count());

            _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnNotFound_WhenNotFound()
        {
            // Arrange
            var serviceResult = new ServiceResult<IEnumerable<UserDto>>(ResultType.NotFound);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnBadRequest_WhenBadRequest()
        {
            // Arrange
            var serviceResult = new ServiceResult<IEnumerable<UserDto>>(ResultType.BadRequest, null);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnUnauthorized_WhenUnauthorized()
        {
            // Arrange
            var serviceResult = new ServiceResult<IEnumerable<UserDto>>(ResultType.Unauthorized);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnConflict_WhenConflict()
        {
            // Arrange
            var conflictData = "Conflict occurred";
            var serviceResult = new ServiceResult<IEnumerable<UserDto>>(ResultType.Conflict, null);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.IsType<object[]>(conflictResult.Value);
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnInternalServerError_WhenUnexpectedResultType()
        {
            // Arrange
            var serviceResult = new ServiceResult<IEnumerable<UserDto>>((ResultType)999); // Unknown result type

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Equal("An unexpected error occurred.", objectResult.Value);
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnOkWithEmptyList_WhenNoUsers()
        {
            // Arrange
            var emptyUsers = new List<UserDto>();
            var serviceResult = new ServiceResult<IEnumerable<UserDto>>(ResultType.Success, emptyUsers);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<UserDto>>(okResult.Value);
            Assert.Empty(returnedUsers);
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldInitializeMediatorCorrectly()
        {
            // Arrange & Act
            var controller = new UserController(_mediatorMock.Object);

            // Assert
            Assert.NotNull(controller);
            Assert.Equal(_mediatorMock.Object, controller._mediator);
        }
    }
}