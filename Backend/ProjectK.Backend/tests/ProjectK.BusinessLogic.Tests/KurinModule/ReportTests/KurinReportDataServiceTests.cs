using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Reports;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Settings;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.ReportTests;

/// <summary>
/// What the звіт куреня says about the склад. Every assertion here has a twin on the реєстр side —
/// the two are read side by side by the same person, and this is where they are kept from drifting.
/// </summary>
public sealed class KurinReportDataServiceTests
{
    private readonly Mock<IKurinReportSource> _source = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();
    private readonly Mock<IKurinReportMedia> _media = new();
    private readonly KurinReportDataService _service;

    private readonly Guid _kurinKey = Guid.NewGuid();
    private readonly Guid _alphaKey = Guid.NewGuid();
    private readonly Guid _betaKey = Guid.NewGuid();
    private readonly Kurin _kurin;
    private readonly Group _alpha;
    private readonly Group _beta;

    private readonly List<Member> _members = [];
    private readonly List<Membership> _memberships = [];
    private readonly List<MentorAssignment> _mentorAssignments = [];

    public KurinReportDataServiceTests()
    {
        _kurin = new Kurin(7) { KurinKey = _kurinKey, Branch = KurinBranch.UPYu };
        _alpha = new Group("Alpha", _kurinKey) { GroupKey = _alphaKey };
        _beta = new Group("Beta", _kurinKey) { GroupKey = _betaKey };

        _media.Setup(m => m.TryDownloadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _service = new KurinReportDataService(
            _source.Object,
            _currentUser.Object,
            new BlobStorageOptions(),
            _media.Object,
            Mock.Of<IProbesCatalogService>(),
            Mock.Of<IBadgesCatalogService>(),
            new ConfigurationBuilder().Build());
    }

    private Member Person(string lastName, Guid? groupKey = null, PlastLevel? level = null, bool withAccount = false)
    {
        var member = new Member
        {
            MemberKey = Guid.NewGuid(),
            UserKey = withAccount ? Guid.NewGuid() : null,
            FirstName = "Тест",
            MiddleName = null,
            LastName = lastName,
            Email = $"{lastName}@example.com",
            PhoneNumber = "0500000000",
            DateOfBirth = new DateOnly(2008, 1, 1),
            LatestPlastLevel = level
        };

        _members.Add(member);
        _memberships.Add(new Membership
        {
            MemberKey = member.MemberKey,
            KurinKey = _kurinKey,
            GroupKey = groupKey,
            JoinedAtUtc = DateTime.UtcNow.AddYears(-1)
        });

        return member;
    }

    private void GiveOffice(
        Member member,
        LeadershipType type,
        LeadershipRole role,
        Guid? groupKey = null,
        DateOnly? heldUntil = null,
        Guid? inKurinKey = null)
    {
        var office = new Leadership
        {
            LeadershipKey = Guid.NewGuid(),
            Type = type,
            KurinKey = type == LeadershipType.Group ? null : inKurinKey ?? _kurinKey,
            GroupKey = groupKey,
            // Без власної назви — так їх і заводять UpsertLeadershipCommand і сідери. Назва, якщо є,
            // перебиває все інше, і фікстура з нею не перевіряла б нічого.
            Name = null,
            StartDate = new DateOnly(2024, 1, 1)
        };

        member.LeadershipHistories.Add(new LeadershipHistory
        {
            LeadershipHistoryKey = Guid.NewGuid(),
            MemberKey = member.MemberKey,
            LeadershipKey = office.LeadershipKey,
            Leadership = office,
            Role = role,
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = heldUntil
        });
    }

    private void MentorOf(Member member, Guid groupKey, DateTime? revokedAtUtc = null)
        => _mentorAssignments.Add(new MentorAssignment
        {
            MentorUserKey = member.UserKey!.Value,
            GroupKey = groupKey,
            AssignedAtUtc = DateTime.UtcNow.AddYears(-1),
            RevokedAtUtc = revokedAtUtc
        });

    private Task<Common.Models.Reports.KurinReportData?> Build(KurinBranch branch = KurinBranch.UPYu)
    {
        _kurin.Branch = branch;
        _source
            .Setup(s => s.LoadAsync(_kurinKey, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KurinReportSourceData(
                _kurin,
                [_alpha, _beta],
                _mentorAssignments,
                _members,
                new Dictionary<Guid, AppUser>(),
                new Dictionary<Guid, IReadOnlyList<ProjectK.Common.Entities.ProbesAndBadgesModule.ProbeProgress>>(),
                new Dictionary<Guid, IReadOnlyList<ProjectK.Common.Entities.ProbesAndBadgesModule.ProbePointProgress>>(),
                new Dictionary<Guid, IReadOnlyList<ProjectK.Common.Entities.ProbesAndBadgesModule.BadgeProgress>>(),
                _memberships.ToDictionary(membership => membership.MemberKey)));

        return _service.BuildAsync(_kurinKey, CancellationToken.None);
    }

    /// <summary>
    /// The same line the реєстр draws: only a КВ office is кадра. It used to be «a Kurin or KV
    /// office, or any global Identity role granting group leadership» — which put the курінний
    /// among the впорядники, and anyone holding a виховник office in a *different* kurin too.
    /// </summary>
    [Fact]
    public async Task ShouldCallOnlyKvOfficersStaff()
    {
        Person("Юнак", _alphaKey);
        var hurtkovyi = Person("Гурткова", _alphaKey);
        var kurinnyi = Person("Курінна", _alphaKey);
        var mentor = Person("Виховна", withAccount: true);
        GiveOffice(hurtkovyi, LeadershipType.Group, LeadershipRole.Hurtkoviy, _alphaKey);
        GiveOffice(kurinnyi, LeadershipType.Kurin, LeadershipRole.Kurinnuy);
        GiveOffice(mentor, LeadershipType.KV, LeadershipRole.Vykhovnyk);

        var report = await Build();

        report!.Staff.Select(member => member.FullName).Should().Equal("Тест Виховна");
        report.Youth.Select(member => member.FullName)
            .Should().BeEquivalentTo("Тест Юнак", "Тест Гурткова", "Тест Курінна");
    }

    [Fact]
    public async Task ShouldNotCallSomeoneStaffOnceTheirTermHasEnded()
    {
        var former = Person("Колишня", withAccount: true);
        GiveOffice(former, LeadershipType.KV, LeadershipRole.Vykhovnyk, heldUntil: new DateOnly(2025, 6, 1));

        var report = await Build();

        report!.Staff.Should().BeEmpty();
        report.Youth.Should().ContainSingle();
    }

    [Fact]
    public async Task ShouldNameTheGroupsEachStaffMemberIsAssignedTo()
    {
        var mentor = Person("Виховна", withAccount: true);
        GiveOffice(mentor, LeadershipType.KV, LeadershipRole.Vykhovnyk);
        MentorOf(mentor, _betaKey);
        MentorOf(mentor, _alphaKey);
        MentorOf(mentor, _alphaKey, revokedAtUtc: DateTime.UtcNow.AddDays(-1));

        var report = await Build();

        report!.Staff.Single().MentoredGroupNames.Should().Equal("Alpha", "Beta");
    }

    [Fact]
    public async Task Tally_ShouldCountYouthByLevel_AndLeaveStaffOut()
    {
        Person("Один", _alphaKey, PlastLevel.Uchasnyk);
        Person("Два", _alphaKey, PlastLevel.Uchasnyk);
        Person("Три", _alphaKey, PlastLevel.Skob);
        var mentor = Person("Виховна", level: PlastLevel.Uchasnyk, withAccount: true);
        GiveOffice(mentor, LeadershipType.KV, LeadershipRole.Vykhovnyk);

        var report = await Build();
        var counts = report!.LevelTally.ToDictionary(row => row.Label, row => row.Count);

        counts["пл. уч."].Should().Be(2, "the виховник is кадра, not юнацтво");
        counts["пл. скоб / вірл."].Should().Be(1);
        counts["Разом"].Should().Be(3);
    }

    [Fact]
    public async Task Tally_ShouldNotSwallowPeopleWithNoLevel()
    {
        Person("Без", _alphaKey);
        Person("Учасник", _alphaKey, PlastLevel.Uchasnyk);

        var report = await Build();
        var counts = report!.LevelTally.ToDictionary(row => row.Label, row => row.Count);

        counts["Без ступеня / інший"].Should().Be(1);
        counts["Разом"].Should().Be(2);
    }

    [Fact]
    public async Task Tally_ShouldFollowTheKurinsBranch()
    {
        Person("Сеньйор", _alphaKey, PlastLevel.SeniorPratsi);

        var report = await Build(KurinBranch.UPS);
        var counts = report!.LevelTally.ToDictionary(row => row.Label, row => row.Count);

        counts.Should().ContainKey("пл. сен. праці");
        counts["пл. сен. праці"].Should().Be(1);
        counts.Should().NotContainKey("Без ступеня / інший");
    }

    /// <summary>
    /// Offices are read along with the person, and from 0.20 a person can be in several kurins. The
    /// звіт is about one of them, so an office held somewhere else has no business being printed
    /// here — nor counting its holder as this kurin's кадра.
    /// </summary>
    [Fact]
    public async Task ShouldIgnoreOfficesHeldInAnotherKurin()
    {
        var visitor = Person("Гостя", _alphaKey, withAccount: true);
        GiveOffice(visitor, LeadershipType.KV, LeadershipRole.Vykhovnyk, inKurinKey: Guid.NewGuid());

        var report = await Build();

        report!.Staff.Should().BeEmpty("the КВ office belongs to a different kurin");
        report.Members.Single().LeadershipHistory.Should().BeEmpty();
    }

    /// <summary>
    /// The звіт names offices the way a провід writes them. The Впорядники table used to print the
    /// identity roles instead — «KV.Vykhovnyk, Member», the very string inside the token.
    /// </summary>
    [Fact]
    public async Task ShouldNameOfficesInWords()
    {
        var mentor = Person("Виховна", withAccount: true);
        GiveOffice(mentor, LeadershipType.KV, LeadershipRole.Vykhovnyk);

        var report = await Build();

        report!.Staff.Single().LeadershipHistory.Single().RoleLabel.Should().Be("Впорядник");
    }

    /// <summary>
    /// A гуртковий's office is scoped to a гурток and says which. A курінний's and a виховник's are
    /// scoped to the kurin the звіт is already about, so they say nothing — they used to print the
    /// literals "Kurin" and "KV", which read as «КВ/Впорядник; KV» in the document.
    /// </summary>
    [Fact]
    public async Task ShouldScopeAnOfficeByItsГурток_AndLeaveKurinWideOnesUnqualified()
    {
        var hurtkovyi = Person("Гурткова", _alphaKey);
        var mentor = Person("Виховна", withAccount: true);
        GiveOffice(hurtkovyi, LeadershipType.Group, LeadershipRole.Hurtkoviy, _alphaKey);
        GiveOffice(mentor, LeadershipType.KV, LeadershipRole.Vykhovnyk);

        var report = await Build();

        report!.Youth.Single(member => member.FullName == "Тест Гурткова")
            .LeadershipHistory.Single().ScopeName.Should().Be("Alpha");
        report.Staff.Single().LeadershipHistory.Single().ScopeName.Should().BeNull();
    }

    /// <summary>
    /// The реєстр reads the newest recorded ступінь and falls back to the stored field. The report
    /// used to read the stored field alone, so a ступінь added with an earlier date — which is how
    /// the roster import writes them — showed one thing on screen and another in the PDF.
    /// </summary>
    [Fact]
    public async Task ShouldReadTheNewestRecordedLevel_NotTheStoredOne()
    {
        var member = Person("Просунулась", _alphaKey, PlastLevel.Uchasnyk);
        member.PlastLevelHistory.Add(new PlastLevelHistory
        {
            MemberKey = member.MemberKey,
            PlastLevel = PlastLevel.Rozviduvach,
            DateAchieved = new DateOnly(2026, 3, 1)
        });

        var report = await Build();

        report!.Youth.Single().LatestPlastLevel.Should().Be(PlastLevel.Rozviduvach);
        report.LevelTally.Single(row => row.Label == "пл. розв.").Count.Should().Be(1);
    }
}
