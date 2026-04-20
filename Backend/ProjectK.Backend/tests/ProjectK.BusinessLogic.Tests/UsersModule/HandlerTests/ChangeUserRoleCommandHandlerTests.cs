using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProjectK.BusinessLogic.Modules.UsersModule.Command;
using ProjectK.BusinessLogic.Modules.UsersModule.Command.Handlers;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace ProjectK.BusinessLogic.Tests.UsersModule.HandlerTests
{
    public class ChangeUserRoleCommandHandlerTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<ICurrentUserContext> _currentUserContextMock;
        private readonly Mock<ILogger<ChangeUserRoleCommandHandler>> _loggerMock;
        private readonly Mock<ProjectK.Common.Interfaces.IUnitOfWork> _unitOfWorkMock;
        private readonly ChangeUserRoleCommandHandler _handler;

        public ChangeUserRoleCommandHandlerTests()
        {
            var store = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
            _currentUserContextMock = new Mock<ICurrentUserContext>();
            _loggerMock = new Mock<ILogger<ChangeUserRoleCommandHandler>>();
            _unitOfWorkMock = new Mock<ProjectK.Common.Interfaces.IUnitOfWork>();

            _handler = new ChangeUserRoleCommandHandler(
                _userManagerMock.Object,
                _currentUserContextMock.Object,
                _loggerMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnForbidden_WhenUserIsNotAdminOrManager()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            _currentUserContextMock.Setup(c => c.IsInRole(UserRole.Admin.ToString())).Returns(false);
            _currentUserContextMock.Setup(c => c.IsInRole(UserRole.Manager.ToString())).Returns(false);

            _userManagerMock.Setup(m => m.FindByIdAsync(targetUserId.ToString()))
                .ReturnsAsync(new AppUser { Id = targetUserId });

            var command = new ChangeUserRoleCommand(targetUserId, UserRole.Manager);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.Forbidden);
            result.Data.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ShouldReturnForbidden_WhenManagerPromotesToAdmin()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            _currentUserContextMock.Setup(c => c.IsInRole(UserRole.Admin.ToString())).Returns(false);
            _currentUserContextMock.Setup(c => c.IsInRole(UserRole.Manager.ToString())).Returns(true);
            _currentUserContextMock.Setup(c => c.KurinKey).Returns(kurinKey);

            _userManagerMock.Setup(m => m.FindByIdAsync(targetUserId.ToString()))
                .ReturnsAsync(new AppUser { Id = targetUserId, KurinKey = kurinKey });

            var command = new ChangeUserRoleCommand(targetUserId, UserRole.Admin);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.Forbidden);
            result.CreatedAtActionName.Should().Contain("cannot promote to Admin");
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenAdminChangesRole()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            _currentUserContextMock.Setup(c => c.IsInRole(UserRole.Admin.ToString())).Returns(true);

            var user = new AppUser { Id = targetUserId };
            _userManagerMock.Setup(m => m.FindByIdAsync(targetUserId.ToString()))
                .ReturnsAsync(user);

            _userManagerMock.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { UserRole.User.ToString() });

            _userManagerMock.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.AddToRoleAsync(user, UserRole.Manager.ToString()))
                .ReturnsAsync(IdentityResult.Success);

            var command = new ChangeUserRoleCommand(targetUserId, UserRole.Manager);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.Success);
            result.Data.Should().BeTrue();
        }
    }
}
