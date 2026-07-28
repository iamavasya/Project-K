using Microsoft.AspNetCore.Identity;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.KurinScope;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.KurinScope.Handlers;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests.KurinScope
{
    public class SetKurinScopeCommandHandlerTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<IKurinRepository> _kurinsMock = new();
        private readonly Mock<ILoginResponseFactory> _loginResponseFactoryMock = new();
        private readonly SetKurinScopeCommandHandler _handler;

        public SetKurinScopeCommandHandlerTests()
        {
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            unitOfWorkMock.SetupGet(x => x.Kurins).Returns(_kurinsMock.Object);

            _loginResponseFactoryMock
                .Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoginUserResponse { Email = "admin@projectk.com", Role = "Admin" });

            _handler = new SetKurinScopeCommandHandler(
                _userManagerMock.Object,
                unitOfWorkMock.Object,
                _loginResponseFactoryMock.Object);
        }

        private AppUser ArrangeUser(params UserRole[] roles)
        {
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = "admin@projectk.com",
                FirstName = "System",
                LastName = "Admin"
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles.Select(role => role.ToClaimValue()).ToList());
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            return user;
        }

        [Fact]
        public async Task Handle_ShouldScopeAdminIntoKurin()
        {
            var user = ArrangeUser(UserRole.Admin);
            var kurinKey = Guid.NewGuid();
            _kurinsMock.Setup(x => x.GetByKeyAsync(kurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Kurin(12) { KurinKey = kurinKey });

            var result = await _handler.Handle(new SetKurinScopeCommand(user.Id, kurinKey), CancellationToken.None);

            Assert.Equal(ResultType.Success, result.Type);
            Assert.Equal(kurinKey, user.ActiveKurinKey);
            _loginResponseFactoryMock.Verify(x => x.CreateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldClearScope_WhenKurinKeyIsNull()
        {
            var user = ArrangeUser(UserRole.Admin);
            user.ActiveKurinKey = Guid.NewGuid();

            var result = await _handler.Handle(new SetKurinScopeCommand(user.Id, null), CancellationToken.None);

            Assert.Equal(ResultType.Success, result.Type);
            Assert.Null(user.ActiveKurinKey);
        }

        [Fact]
        public async Task Handle_ShouldForbidNonAdmin()
        {
            var user = ArrangeUser(UserRole.Manager);
            var kurinKey = Guid.NewGuid();

            var result = await _handler.Handle(new SetKurinScopeCommand(user.Id, kurinKey), CancellationToken.None);

            Assert.Equal(ResultType.Forbidden, result.Type);
            Assert.Null(user.ActiveKurinKey);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenKurinDoesNotExist()
        {
            var user = ArrangeUser(UserRole.Admin);
            var kurinKey = Guid.NewGuid();
            _kurinsMock.Setup(x => x.GetByKeyAsync(kurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Kurin?)null);

            var result = await _handler.Handle(new SetKurinScopeCommand(user.Id, kurinKey), CancellationToken.None);

            Assert.Equal(ResultType.NotFound, result.Type);
            Assert.Null(user.ActiveKurinKey);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenUserIsMissing()
        {
            var missingKey = Guid.NewGuid();
            _userManagerMock.Setup(x => x.FindByIdAsync(missingKey.ToString())).ReturnsAsync((AppUser?)null);

            var result = await _handler.Handle(new SetKurinScopeCommand(missingKey, null), CancellationToken.None);

            Assert.Equal(ResultType.Unauthorized, result.Type);
        }
    }
}
