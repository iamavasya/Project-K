using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
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
    private readonly Mock<IMembershipDirectory> _memberships = new();
    private readonly AccessContextResolver _resolver;

    public AccessContextResolverTests()
    {
        _userManager = new Mock<UserManager<AppUser>>(
            new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);
        _memberships
            .Setup(d => d.GetCurrentForAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _resolver = new AccessContextResolver(_userManager.Object, _offices.Object, _memberships.Object);
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

    /// <summary>Belongs to one kurin and has never used the switcher — that kurin is where they are.</summary>
    [Fact]
    public async Task NeverHavingChosen_ShouldStandWhereTheirOneMembershipIs()
    {
        var kurinKey = Guid.NewGuid();
        var user = Account();
        BelongsTo(user, (kurinKey, DateTime.UtcNow.AddYears(-1)));
        HoldsInKurin(user, kurinKey, new MemberOffice(LeadershipType.KV, LeadershipRole.Vykhovnyk));

        var access = await _resolver.ResolveAsync(user);

        access.KurinKey.Should().Be(kurinKey);
        access.Roles.Should().Contain(SystemRole.ForOffice(LeadershipType.KV, LeadershipRole.Vykhovnyk));
    }

    /// <summary>An explicit choice is never second-guessed by the memberships.</summary>
    [Fact]
    public async Task HavingChosen_ShouldBeLeftWhereTheyPutThemselves()
    {
        var chosen = Guid.NewGuid();
        var user = Account(activeKurinKey: chosen);
        BelongsTo(user, (Guid.NewGuid(), DateTime.UtcNow));
        HoldsInKurin(user, chosen);

        var access = await _resolver.ResolveAsync(user);

        access.KurinKey.Should().Be(chosen);
    }

    /// <summary>
    /// A виховник of one kurin and a plain member of a newer one belongs, by default, among the
    /// people they answer for — not wherever they happened to join last.
    /// </summary>
    [Fact]
    public async Task BelongingToSeveral_ShouldStandWhereTheyHoldAnOffice()
    {
        var withOffice = Guid.NewGuid();
        var joinedLater = Guid.NewGuid();
        var user = Account();
        BelongsTo(user, (withOffice, DateTime.UtcNow.AddYears(-3)), (joinedLater, DateTime.UtcNow.AddMonths(-1)));
        HoldsInKurin(user, withOffice, new MemberOffice(LeadershipType.KV, LeadershipRole.Vykhovnyk));
        HoldsInKurin(user, joinedLater);

        var access = await _resolver.ResolveAsync(user);

        access.KurinKey.Should().Be(withOffice);
        access.Roles.Should().Contain(SystemRole.ForOffice(LeadershipType.KV, LeadershipRole.Vykhovnyk));
    }

    /// <summary>Holding nothing anywhere: the newest, so they land somewhere real rather than nowhere.</summary>
    [Fact]
    public async Task BelongingToSeveralWithoutOffices_ShouldStandInTheNewest()
    {
        var older = Guid.NewGuid();
        var newer = Guid.NewGuid();
        var user = Account();
        BelongsTo(user, (older, DateTime.UtcNow.AddYears(-3)), (newer, DateTime.UtcNow.AddMonths(-1)));
        HoldsInKurin(user, older);
        HoldsInKurin(user, newer);

        var access = await _resolver.ResolveAsync(user);

        access.KurinKey.Should().Be(newer);
    }

    /// <summary>Belonging nowhere is still an answer — an admin between kurins, or a fresh account.</summary>
    [Fact]
    public async Task BelongingNowhere_ShouldStayNowhere()
    {
        var user = Account();

        var access = await _resolver.ResolveAsync(user);

        access.KurinKey.Should().BeNull();
        access.Roles.Should().Equal(SystemRole.Member);
    }

    private void BelongsTo(AppUser user, params (Guid KurinKey, DateTime JoinedAtUtc)[] memberships) =>
        _memberships
            .Setup(d => d.GetCurrentForAccountAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. memberships.Select(m => new MembershipRecord(
                Guid.NewGuid(), m.KurinKey, 1, KurinBranch.UPYu, null, null, null,
                MembershipKind.Youth, m.JoinedAtUtc, null))]);
}
