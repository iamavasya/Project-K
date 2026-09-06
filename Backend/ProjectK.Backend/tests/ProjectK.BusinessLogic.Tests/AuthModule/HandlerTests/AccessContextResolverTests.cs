using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests;

/// <summary>
/// One answer to "what may this person do", and it is always about one kurin. Admin is the only
/// thing the identity store still contributes; every office comes from the kurin being asked about.
/// </summary>
public class AccessContextResolverTests
{
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly Mock<IOfficeDirectory> _offices = new();
    private readonly AccessContextResolver _resolver;

    public AccessContextResolverTests()
    {
        _userManager = new Mock<UserManager<AppUser>>(
            new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);
        _resolver = new AccessContextResolver(_userManager.Object, _offices.Object);
    }

    private AppUser Account(Guid? kurinKey = null, Guid? activeKurinKey = null, bool isAdmin = false)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "person@example.com",
            FirstName = "Оксана",
            LastName = "Тестова",
            KurinKey = kurinKey,
            ActiveKurinKey = activeKurinKey
        };

        _userManager.Setup(m => m.IsInRoleAsync(user, SystemRole.Admin)).ReturnsAsync(isAdmin);
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        return user;
    }

    private void HoldsInKurin(AppUser user, Guid kurinKey, params MemberOffice[] offices) =>
        _offices
            .Setup(d => d.GetForAccountInKurinAsync(user.Id, kurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(offices);

    [Fact]
    public async Task EveryAccount_ShouldCarryTheBaseline()
    {
        var user = Account(kurinKey: Guid.NewGuid());
        HoldsInKurin(user, user.KurinKey!.Value);

        var access = await _resolver.ResolveAsync(user);

        access.Roles.Should().Equal(SystemRole.Member);
        access.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task AnOfficeInTheKurinTheyAreIn_ShouldBecomeARole()
    {
        var kurinKey = Guid.NewGuid();
        var user = Account(kurinKey: kurinKey);
        HoldsInKurin(user, kurinKey, new MemberOffice(LeadershipType.KV, LeadershipRole.Vykhovnyk));

        var access = await _resolver.ResolveAsync(user);

        access.KurinKey.Should().Be(kurinKey);
        access.Roles.Should().BeEquivalentTo(
            [SystemRole.Member, SystemRole.ForOffice(LeadershipType.KV, LeadershipRole.Vykhovnyk)]);
    }

    [Fact]
    public async Task TheKurinAskedAbout_ShouldBeTheOneSteppedInto_NotTheAccountsOwn()
    {
        var ownKurin = Guid.NewGuid();
        var steppedInto = Guid.NewGuid();
        var user = Account(kurinKey: ownKurin, activeKurinKey: steppedInto, isAdmin: true);
        HoldsInKurin(user, ownKurin, new MemberOffice(LeadershipType.KV, LeadershipRole.Zvyazkovyi));
        HoldsInKurin(user, steppedInto);

        var access = await _resolver.ResolveAsync(user);

        access.KurinKey.Should().Be(steppedInto);
        access.Roles.Should().BeEquivalentTo([SystemRole.Member, SystemRole.Admin]);
        _offices.Verify(
            d => d.GetForAccountInKurinAsync(user.Id, ownKurin, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AnAdminOutsideEveryKurin_ShouldStillBeAnAdmin_AndHoldNoOffice()
    {
        var user = Account(isAdmin: true);

        var access = await _resolver.ResolveAsync(user);

        access.KurinKey.Should().BeNull();
        access.IsAdmin.Should().BeTrue();
        access.Roles.Should().BeEquivalentTo([SystemRole.Member, SystemRole.Admin]);
        _offices.Verify(
            d => d.GetForAccountInKurinAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AnAccountThatIsGone_ShouldResolveToNothing()
    {
        var missing = Guid.NewGuid();
        _userManager.Setup(m => m.FindByIdAsync(missing.ToString())).ReturnsAsync((AppUser?)null);

        var access = await _resolver.ResolveAsync(missing);

        access.KurinKey.Should().BeNull();
        access.Roles.Should().Equal(SystemRole.Member);
    }
}
