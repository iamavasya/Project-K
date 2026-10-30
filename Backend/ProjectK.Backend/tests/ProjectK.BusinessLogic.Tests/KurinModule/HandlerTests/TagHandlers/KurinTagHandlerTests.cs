using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Tag;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule.Tags;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Audit;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Tags;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.TagHandlers;

public class KurinTagHandlerTests
{
    private static readonly Guid KurinKey = Guid.NewGuid();
    private static readonly Guid MemberKey = Guid.NewGuid();

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IKurinTagRepository> _tags = new();
    private readonly Mock<IMembershipRepository> _memberships = new();
    private readonly Mock<IKurinTagVisibility> _visibility = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();
    private readonly List<AuditRecord> _journal = [];
    private readonly Mock<IKurinAudit> _audit = new();

    public KurinTagHandlerTests()
    {
        _uow.Setup(u => u.KurinTags).Returns(_tags.Object);
        _uow.Setup(u => u.Memberships).Returns(_memberships.Object);
        _currentUser.Setup(c => c.KurinKey).Returns(KurinKey);
        _audit.Setup(a => a.RecordAsync(It.IsAny<AuditRecord>(), It.IsAny<CancellationToken>()))
            .Callback<AuditRecord, CancellationToken>((record, _) => _journal.Add(record))
            .Returns(Task.CompletedTask);
        TagsAre(enabled: true);
        InKurin(MemberKey);
    }

    [Fact]
    public void Tags_AreOnByDefault_OnlyInASeniorKurin()
    {
        KurinTagRules.EnabledByDefault(KurinBranch.UPYu).Should().BeFalse();
        KurinTagRules.EnabledByDefault(KurinBranch.USP).Should().BeTrue();
        KurinTagRules.EnabledByDefault(KurinBranch.UPS).Should().BeTrue();
    }

    [Fact]
    public async Task SetMemberTags_PutsOnAndTakesOff_AndJournalsEachAboutThePerson()
    {
        var kept = Tag("Новики");
        var gone = Tag("Табір 2026");
        var added = Tag("Рада куреня");
        _tags.Setup(t => t.GetForKurinAsync(KurinKey, It.IsAny<CancellationToken>())).ReturnsAsync([kept, gone, added]);
        var carrying = new List<MemberTag> { On(kept), On(gone) };
        _tags.Setup(t => t.GetTrackedForMemberAsync(KurinKey, MemberKey, It.IsAny<CancellationToken>())).ReturnsAsync(carrying);

        var result = await MemberTagsHandler().Handle(new SetMemberTagsCommand(MemberKey, [kept.KurinTagKey, added.KurinTagKey]), default);

        result.Type.Should().Be(ResultType.Success);
        _tags.Verify(t => t.AddAssignment(It.Is<MemberTag>(a => a.KurinTagKey == added.KurinTagKey && a.MemberKey == MemberKey)), Times.Once);
        _tags.Verify(t => t.RemoveAssignment(It.Is<MemberTag>(a => a.KurinTagKey == gone.KurinTagKey)), Times.Once);
        _journal.Select(entry => (entry.Action, entry.SubjectLabel, entry.SubjectMemberKey)).Should().BeEquivalentTo(new[]
        {
            (AuditActions.Tag.Put, "Рада куреня", (Guid?)MemberKey),
            (AuditActions.Tag.TakenOff, "Табір 2026", (Guid?)MemberKey)
        });
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetMemberTags_RefusesMoreThanTheLimit()
    {
        var keys = Enumerable.Range(0, KurinTagRules.MaxTagsPerMember + 1).Select(_ => Guid.NewGuid()).ToList();

        var result = await MemberTagsHandler().Handle(new SetMemberTagsCommand(MemberKey, keys), default);

        result.ErrorCode.Should().Be(KurinTagErrors.TooManyTags);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetMemberTags_IsRefused_WhileTheKurinHasTagsOff()
    {
        TagsAre(enabled: false);

        var result = await MemberTagsHandler().Handle(new SetMemberTagsCommand(MemberKey, []), default);

        result.ErrorCode.Should().Be(KurinTagErrors.TagsDisabled);
    }

    [Fact]
    public async Task SetTagMembers_RefusesSomeoneOutsideTheKurin()
    {
        var tag = Tag("Новики");
        _tags.Setup(t => t.GetWithMembersAsync(tag.KurinTagKey, It.IsAny<CancellationToken>())).ReturnsAsync(tag);

        var result = await TagMembersHandler().Handle(new SetTagMembersCommand(KurinKey, tag.KurinTagKey, [MemberKey, Guid.NewGuid()]), default);

        result.ErrorCode.Should().Be(KurinTagErrors.NotInKurin);
        _tags.Verify(t => t.AddAssignment(It.IsAny<MemberTag>()), Times.Never);
    }

    [Fact]
    public async Task SetTagMembers_RefusesWhoeverAlreadyCarriesTheLimit()
    {
        var tag = Tag("Новики");
        _tags.Setup(t => t.GetWithMembersAsync(tag.KurinTagKey, It.IsAny<CancellationToken>())).ReturnsAsync(tag);
        _tags.Setup(t => t.CountPerMemberAsync(KurinKey, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [MemberKey] = KurinTagRules.MaxTagsPerMember });

        var result = await TagMembersHandler().Handle(new SetTagMembersCommand(KurinKey, tag.KurinTagKey, [MemberKey]), default);

        result.ErrorCode.Should().Be(KurinTagErrors.TooManyTags);
    }

    [Fact]
    public async Task SetTagMembers_TakesTheTagOffWhoeverIsLeftOut()
    {
        var tag = Tag("Новики");
        var other = Guid.NewGuid();
        tag.Members.Add(new MemberTag { KurinTagKey = tag.KurinTagKey, MemberKey = other });
        _tags.Setup(t => t.GetWithMembersAsync(tag.KurinTagKey, It.IsAny<CancellationToken>())).ReturnsAsync(tag);
        _tags.Setup(t => t.CountPerMemberAsync(KurinKey, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await TagMembersHandler().Handle(new SetTagMembersCommand(KurinKey, tag.KurinTagKey, [MemberKey]), default);

        result.Data!.MemberCount.Should().Be(1);
        _tags.Verify(t => t.RemoveAssignment(It.Is<MemberTag>(a => a.MemberKey == other)), Times.Once);
        _journal.Select(entry => entry.Action).Should().BeEquivalentTo([AuditActions.Tag.Put, AuditActions.Tag.TakenOff]);
    }

    [Fact]
    public async Task SetSettings_StoresTheBranchDefaultAsNoChoice()
    {
        _visibility.Setup(v => v.SettingsAsync(KurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KurinTagSettingsState(Enabled: true, EnabledByDefault: true, OwnTagsVisible: true));
        var stored = new KurinTagSettings { KurinKey = KurinKey, Enabled = false };
        _tags.Setup(t => t.GetSettingsAsync(KurinKey, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        await new SetKurinTagSettingsCommandHandler(_uow.Object, _visibility.Object, _audit.Object)
            .Handle(new SetKurinTagSettingsCommand(KurinKey, Enabled: true, OwnTagsVisible: false), default);

        stored.Enabled.Should().BeNull("on is what a senior kurin has anyway, so it keeps following its branch");
        stored.OwnTagsVisible.Should().BeFalse();
        _journal.Single().Action.Should().Be(AuditActions.Settings.TagSettingsChanged);
    }

    private SetMemberTagsCommandHandler MemberTagsHandler() =>
        new(_uow.Object, _visibility.Object, _audit.Object, _currentUser.Object);

    private SetTagMembersCommandHandler TagMembersHandler() =>
        new(_uow.Object, _visibility.Object, _audit.Object);

    private void TagsAre(bool enabled) =>
        _visibility.Setup(v => v.SettingsAsync(KurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KurinTagSettingsState(enabled, EnabledByDefault: false, OwnTagsVisible: true));

    private void InKurin(params Guid[] memberKeys) =>
        _memberships.Setup(m => m.GetRecordsInKurinAsync(KurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberKeys
                .Select(key => new KurinMembershipRecord(Guid.NewGuid(), key, null, MembershipKind.Youth, DateTime.UtcNow, null))
                .ToList());

    private static KurinTag Tag(string name) => new() { KurinKey = KurinKey, Name = name, ColorHex = KurinTagRules.Palette[0] };

    private static MemberTag On(KurinTag tag) => new() { KurinTagKey = tag.KurinTagKey, MemberKey = MemberKey };
}
