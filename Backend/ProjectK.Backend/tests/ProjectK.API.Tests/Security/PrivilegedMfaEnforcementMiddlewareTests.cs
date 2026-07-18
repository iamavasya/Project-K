using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using ProjectK.API.Middleware;
using ProjectK.API.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Tests.Security;

public class PrivilegedMfaEnforcementMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldContinue_WhenPrivilegedUserReadsPageDataWithoutMfa()
    {
        // Arrange
        var userKey = Guid.NewGuid();
        var context = CreateContext("/api/user/users", userKey, UserRole.Manager);
        context.Request.Method = HttpMethods.Get;
        var nextCalled = false;
        var middleware = new PrivilegedMfaEnforcementMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var userManagerMock = CreateUserManagerMock();

        // Act
        await middleware.InvokeAsync(context, userManagerMock.Object, CreatePolicy(required: true).Object);

        // Assert
        Assert.True(nextCalled);
        userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldContinue_WhenPrivilegedUserHasMfa()
    {
        // Arrange
        var userKey = Guid.NewGuid();
        var context = CreateContext("/api/user/users", userKey, UserRole.Admin);
        var nextCalled = false;
        var middleware = new PrivilegedMfaEnforcementMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(x => x.FindByIdAsync(userKey.ToString()))
            .ReturnsAsync(new AppUser { Id = userKey, TwoFactorEnabled = true });

        // Act
        await middleware.InvokeAsync(context, userManagerMock.Object, CreatePolicy(required: true).Object);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ShouldContinue_WhenPrivilegedUserCallsMfaSetupEndpoint()
    {
        // Arrange
        var userKey = Guid.NewGuid();
        var context = CreateContext("/api/auth/mfa/setup", userKey, UserRole.Manager);
        var nextCalled = false;
        var middleware = new PrivilegedMfaEnforcementMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var userManagerMock = CreateUserManagerMock();

        // Act
        await middleware.InvokeAsync(context, userManagerMock.Object, CreatePolicy(required: true).Object);

        // Assert
        Assert.True(nextCalled);
        userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldContinue_WhenPrivilegedUserReadsOwnAccountSettings()
    {
        // Arrange
        var userKey = Guid.NewGuid();
        var context = CreateContext("/api/user/me", userKey, UserRole.Manager);
        context.Request.Method = HttpMethods.Get;
        var nextCalled = false;
        var middleware = new PrivilegedMfaEnforcementMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var userManagerMock = CreateUserManagerMock();

        // Act
        await middleware.InvokeAsync(context, userManagerMock.Object, CreatePolicy(required: true).Object);

        // Assert
        Assert.True(nextCalled);
        userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldContinue_WhenPrivilegedUserChecksAccessWithoutMfa()
    {
        // Arrange
        var userKey = Guid.NewGuid();
        var context = CreateContext("/api/auth/check-access", userKey, UserRole.Manager);
        context.Request.Method = HttpMethods.Post;
        var nextCalled = false;
        var middleware = new PrivilegedMfaEnforcementMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var userManagerMock = CreateUserManagerMock();

        // Act
        await middleware.InvokeAsync(context, userManagerMock.Object, CreatePolicy(required: true).Object);

        // Assert
        Assert.True(nextCalled);
        userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnForbidden_WhenPrivilegedUserUpdatesOwnProfileWithoutMfa()
    {
        // Arrange
        var userKey = Guid.NewGuid();
        var context = CreateContext("/api/user/me", userKey, UserRole.Manager);
        context.Request.Method = HttpMethods.Put;
        var nextCalled = false;
        var middleware = new PrivilegedMfaEnforcementMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(x => x.FindByIdAsync(userKey.ToString()))
            .ReturnsAsync(new AppUser { Id = userKey, TwoFactorEnabled = false });

        // Act
        await middleware.InvokeAsync(context, userManagerMock.Object, CreatePolicy(required: true).Object);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldContinue_WhenUserIsNotPrivileged()
    {
        // Arrange
        var userKey = Guid.NewGuid();
        var context = CreateContext("/api/user/users", userKey, UserRole.User);
        var nextCalled = false;
        var middleware = new PrivilegedMfaEnforcementMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var userManagerMock = CreateUserManagerMock();

        // Act
        await middleware.InvokeAsync(context, userManagerMock.Object, CreatePolicy(required: true).Object);

        // Assert
        Assert.True(nextCalled);
        userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldContinue_WhenPolicyDoesNotRequireMfa()
    {
        // Arrange (e.g. self-host with enforcement disabled, or Development)
        var userKey = Guid.NewGuid();
        var context = CreateContext("/api/user/me", userKey, UserRole.Manager);
        context.Request.Method = HttpMethods.Put;
        var nextCalled = false;
        var middleware = new PrivilegedMfaEnforcementMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var userManagerMock = CreateUserManagerMock();

        // Act
        await middleware.InvokeAsync(context, userManagerMock.Object, CreatePolicy(required: false).Object);

        // Assert
        Assert.True(nextCalled);
        userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    private static DefaultHttpContext CreateContext(string path, Guid userKey, UserRole role)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userKey.ToString()),
                new Claim(ClaimTypes.Role, role.ToString())
            ],
            "Test"));

        return context;
    }

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
    }

    private static Mock<IMfaEnforcementPolicy> CreatePolicy(bool required)
    {
        var mock = new Mock<IMfaEnforcementPolicy>();
        mock.Setup(x => x.IsPrivilegedMfaRequiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(required);
        return mock;
    }
}
