using Microsoft.AspNetCore.Identity;
using Moq;
using ProjectK.BusinessLogic.Modules.UsersModule.Models;
using ProjectK.BusinessLogic.Modules.UsersModule.Queries;
using ProjectK.BusinessLogic.Modules.UsersModule.Queries.Handlers;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Enums;
using System.Linq.Expressions;

namespace ProjectK.BusinessLogic.Tests.UsersModule.HandlerTests
{
    public class GetAllUsersQueryHandlerTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly GetAllUsersQueryHandler _handler;

        public GetAllUsersQueryHandlerTests()
        {
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);
            _handler = new GetAllUsersQueryHandler(_userManagerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenUsersExist()
        {
            // Arrange
            var users = new List<AppUser>
            {
                new AppUser
                {
                    Id = Guid.NewGuid(),
                    Email = "user1@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    KurinKey = Guid.NewGuid()
                },
                new AppUser
                {
                    Id = Guid.NewGuid(),
                    Email = "user2@example.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    KurinKey = Guid.NewGuid()
                }
            }.AsQueryable();

            var query = new GetAllUsersQuery();

            SetupUserManagerUsers(users);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(new List<string> { "User" });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count());

            var userList = result.Data.ToList();
            Assert.Equal("user1@example.com", userList[0].Email);
            Assert.Equal("John", userList[0].FirstName);
            Assert.Equal("Doe", userList[0].LastName);
            Assert.Equal("User", userList[0].Role);

            Assert.Equal("user2@example.com", userList[1].Email);
            Assert.Equal("Jane", userList[1].FirstName);
            Assert.Equal("Smith", userList[1].LastName);
            Assert.Equal("User", userList[1].Role);

            _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<AppUser>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccessWithEmptyList_WhenNoUsersExist()
        {
            // Arrange
            var users = new List<AppUser>().AsQueryable();
            var query = new GetAllUsersQuery();

            SetupUserManagerUsers(users);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);

            _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldHandleUsersWithMultipleRoles()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var users = new List<AppUser>
            {
                new AppUser
                {
                    Id = userId,
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    KurinKey = Guid.NewGuid()
                }
            }.AsQueryable();

            var query = new GetAllUsersQuery();

            SetupUserManagerUsers(users);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<AppUser>(u => u.Id == userId)))
                .ReturnsAsync(new List<string> { "Admin", "Manager", "User" });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.Single(result.Data);
            Assert.Equal("Admin", result.Data.First().Role); // Should return first role
        }

        [Fact]
        public async Task Handle_ShouldHandleUsersWithNoRoles()
        {
            // Arrange
            var users = new List<AppUser>
            {
                new AppUser
                {
                    Id = Guid.NewGuid(),
                    Email = "noroles@example.com",
                    FirstName = "No",
                    LastName = "Roles",
                    KurinKey = Guid.NewGuid()
                }
            }.AsQueryable();

            var query = new GetAllUsersQuery();

            SetupUserManagerUsers(users);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(new List<string>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.Single(result.Data);
            Assert.Null(result.Data.First().Role); // FirstOrDefault on empty list returns null
        }

        [Fact]
        public async Task Handle_ShouldHandleUsersWithNullKurinKey()
        {
            // Arrange
            var users = new List<AppUser>
            {
                new AppUser
                {
                    Id = Guid.NewGuid(),
                    Email = "nullkurin@example.com",
                    FirstName = "Null",
                    LastName = "Kurin",
                    KurinKey = null
                }
            }.AsQueryable();

            var query = new GetAllUsersQuery();

            SetupUserManagerUsers(users);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(new List<string> { "User" });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.Single(result.Data);
            Assert.Null(result.Data.First().KurinKey);
        }

        [Fact]
        public async Task Handle_ShouldHandleDifferentUserRoles()
        {
            // Arrange
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();
            var user3Id = Guid.NewGuid();

            var users = new List<AppUser>
            {
                new AppUser
                {
                    Id = user1Id,
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    KurinKey = Guid.NewGuid()
                },
                new AppUser
                {
                    Id = user2Id,
                    Email = "manager@example.com",
                    FirstName = "Manager",
                    LastName = "User",
                    KurinKey = Guid.NewGuid()
                },
                new AppUser
                {
                    Id = user3Id,
                    Email = "regular@example.com",
                    FirstName = "Regular",
                    LastName = "User",
                    KurinKey = Guid.NewGuid()
                }
            }.AsQueryable();

            var query = new GetAllUsersQuery();

            SetupUserManagerUsers(users);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<AppUser>(u => u.Id == user1Id)))
                .ReturnsAsync(new List<string> { "Admin" });
            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<AppUser>(u => u.Id == user2Id)))
                .ReturnsAsync(new List<string> { "Manager" });
            _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<AppUser>(u => u.Id == user3Id)))
                .ReturnsAsync(new List<string> { "User" });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.Equal(3, result.Data.Count());

            var userList = result.Data.ToList();
            Assert.Equal("Admin", userList[0].Role);
            Assert.Equal("Manager", userList[1].Role);
            Assert.Equal("User", userList[2].Role);
        }

        [Fact]
        public async Task Handle_ShouldMapAllUserProperties()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var users = new List<AppUser>
            {
                new AppUser
                {
                    Id = userId,
                    Email = "test@example.com",
                    FirstName = "Test",
                    LastName = "User",
                    KurinKey = kurinKey
                }
            }.AsQueryable();

            var query = new GetAllUsersQuery();

            SetupUserManagerUsers(users);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(new List<string> { "TestRole" });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.Single(result.Data);

            var userDto = result.Data.First();
            Assert.Equal(userId, userDto.UserId);
            Assert.Equal(kurinKey, userDto.KurinKey);
            Assert.Equal("test@example.com", userDto.Email);
            Assert.Equal("Test", userDto.FirstName);
            Assert.Equal("User", userDto.LastName);
            Assert.Equal("TestRole", userDto.Role);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccessEvenWhenResultIsNull()
        {
            // Arrange
            var users = new List<AppUser>().AsQueryable();
            var query = new GetAllUsersQuery();

            SetupUserManagerUsers(users);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task Handle_ShouldCallGetRolesAsyncForEachUser()
        {
            // Arrange
            var users = new List<AppUser>
            {
                new AppUser { Id = Guid.NewGuid(), Email = "user1@test.com", FirstName = "User1", LastName = "Test" },
                new AppUser { Id = Guid.NewGuid(), Email = "user2@test.com", FirstName = "User2", LastName = "Test" },
                new AppUser { Id = Guid.NewGuid(), Email = "user3@test.com", FirstName = "User3", LastName = "Test" }
            }.AsQueryable();

            var query = new GetAllUsersQuery();

            SetupUserManagerUsers(users);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(new List<string> { "User" });

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<AppUser>()), Times.Exactly(3));
        }

        [Fact]
        public async Task Handle_ShouldHandleLargeNumberOfUsers()
        {
            // Arrange
            var users = new List<AppUser>();
            for (int i = 0; i < 100; i++)
            {
                users.Add(new AppUser
                {
                    Id = Guid.NewGuid(),
                    Email = $"user{i}@example.com",
                    FirstName = $"User{i}",
                    LastName = "Test",
                    KurinKey = Guid.NewGuid()
                });
            }

            var query = new GetAllUsersQuery();

            SetupUserManagerUsers(users.AsQueryable());
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(new List<string> { "User" });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultType.Success, result.Type);
            Assert.Equal(100, result.Data.Count());
            _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<AppUser>()), Times.Exactly(100));
        }

        [Fact]
        public void Constructor_ShouldInitializeUserManagerCorrectly()
        {
            // Arrange & Act
            var handler = new GetAllUsersQueryHandler(_userManagerMock.Object);

            // Assert
            Assert.NotNull(handler);
        }

        private void SetupUserManagerUsers(IQueryable<AppUser> users)
        {
            _userManagerMock.Setup(x => x.Users).Returns(users);
        }
    }
}