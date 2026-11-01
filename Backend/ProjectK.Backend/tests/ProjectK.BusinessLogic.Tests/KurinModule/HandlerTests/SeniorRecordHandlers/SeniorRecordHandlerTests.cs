using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.BusinessLogic.MappingProfiles;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.SeniorRecord;
using ProjectK.BusinessLogic.Tests.TestHelpers;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Catalog;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.SeniorRecordHandlers;

/// <summary>
/// The книжечка is the person's own: they write it without anyone confirming, and nobody writes
/// someone else's — not even who leads their kurin.
/// </summary>
public class SeniorRecordHandlerTests
{
    private readonly Mock<IMemberUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMemberRepository> _members = new();
    private readonly Mock<IMemberSeniorRecordRepository> _records = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();
    private readonly Mock<ISeniorRecordsCatalog> _catalog = new();
    private readonly List<MemberSeniorRecord> _stored = [];
    private readonly Guid _userKey = Guid.NewGuid();
    private readonly Guid _memberKey = Guid.NewGuid();
    private readonly UpsertMySeniorRecordCommandHandler _upsert;
    private readonly DeleteMySeniorRecordCommandHandler _delete;

    public SeniorRecordHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Members).Returns(_members.Object);
        _unitOfWork.SetupGet(u => u.MemberSeniorRecords).Returns(_records.Object);
        _currentUser.SetupGet(c => c.UserId).Returns(_userKey);
        _members.Setup(m => m.GetSummaryByUserKeyAsync(_userKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemberSummary(_memberKey, _userKey, Guid.NewGuid(), null, "Остап", "Гнатюк", "o@example.com", null));
        _records.Setup(r => r.GetByMemberKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid key, CancellationToken _) => _stored.Where(r => r.MemberKey == key).ToList());
        _records.Setup(r => r.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid key, CancellationToken _) => _stored.FirstOrDefault(r => r.MemberSeniorRecordKey == key));
        _records.Setup(r => r.Create(It.IsAny<MemberSeniorRecord>(), It.IsAny<CancellationToken>()))
            .Callback((MemberSeniorRecord record, CancellationToken _) => _stored.Add(record));

        foreach (var item in new[]
        {
            new SeniorCatalogItem("usp-starshyi-plastun", SeniorRecordKind.Degree, "Старший пластун", "usp", "usp", 1, ""),
            new SeniorCatalogItem("usp-hramota-podiaky", SeniorRecordKind.Award, "Грамота подяки", "usp-vidznachennia", "usp", 0, ""),
            new SeniorCatalogItem("lsh-absolvent", SeniorRecordKind.Training, "Лісова Школа — абсольвент", "lsh", "usp", 1, "")
        })
        {
            _catalog.Setup(c => c.GetById(item.Id)).Returns(item);
        }

        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.ConstructServicesUsing(FakeBlobReadLinks.Resolvers);
            cfg.AddProfile(new KurinModuleProfile());
        }, LoggerFactory.Create(_ => { })).CreateMapper();

        _upsert = new UpsertMySeniorRecordCommandHandler(_unitOfWork.Object, _currentUser.Object, _catalog.Object, mapper);
        _delete = new DeleteMySeniorRecordCommandHandler(_unitOfWork.Object, _currentUser.Object);
    }

    private Task<ServiceResult<Common.Models.Dtos.KurinModule.MemberSeniorRecordDto>> Add(string catalogId, string? number = null) =>
        _upsert.Handle(new UpsertMySeniorRecordCommand { CatalogId = catalogId, Number = number, AchievedOn = new DateOnly(2020, 5, 1) }, CancellationToken.None);

    [Fact]
    public async Task APersonEntersTheirOwnLine_WithNobodyToConfirmIt()
    {
        var result = await Add("lsh-absolvent", " ЛШ 41 ");

        result.Type.Should().Be(ResultType.Created);
        var record = _stored.Should().ContainSingle().Subject;
        record.MemberKey.Should().Be(_memberKey);
        record.Number.Should().Be("ЛШ 41");
        result.Data!.CatalogId.Should().Be("lsh-absolvent");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnlyWhatTheCatalogueHas_CanBeEntered()
    {
        var result = await Add("upyu-vidznachennia-1");

        result.Type.Should().Be(ResultType.BadRequest);
        _stored.Should().BeEmpty();
    }

    [Fact]
    public async Task AStupinIsEnteredOnce_AnAwardMayComeAgain()
    {
        (await Add("usp-starshyi-plastun")).Type.Should().Be(ResultType.Created);
        (await Add("usp-starshyi-plastun")).Type.Should().Be(ResultType.Conflict);

        (await Add("usp-hramota-podiaky")).Type.Should().Be(ResultType.Created);
        (await Add("usp-hramota-podiaky")).Type.Should().Be(ResultType.Created);
    }

    [Fact]
    public async Task SomeoneElsesLine_AnswersAsMissing_ForAChangeAndForARemoval()
    {
        var theirs = new MemberSeniorRecord { MemberKey = Guid.NewGuid(), CatalogId = "lsh-absolvent", Number = "ЛШ 3" };
        _stored.Add(theirs);

        var change = await _upsert.Handle(
            new UpsertMySeniorRecordCommand { MemberSeniorRecordKey = theirs.MemberSeniorRecordKey, CatalogId = "lsh-absolvent", Number = "ЛШ 99" },
            CancellationToken.None);
        var removal = await _delete.Handle(new DeleteMySeniorRecordCommand(theirs.MemberSeniorRecordKey), CancellationToken.None);

        change.Type.Should().Be(ResultType.NotFound);
        removal.Type.Should().Be(ResultType.NotFound);
        theirs.Number.Should().Be("ЛШ 3");
        _records.Verify(r => r.Delete(It.IsAny<MemberSeniorRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OwnLine_CanBeChangedAndRemoved()
    {
        await Add("lsh-absolvent", "ЛШ 41");
        var mine = _stored.Single();

        var change = await _upsert.Handle(
            new UpsertMySeniorRecordCommand { MemberSeniorRecordKey = mine.MemberSeniorRecordKey, CatalogId = "lsh-absolvent", Number = "ЛШ 42", Note = "  " },
            CancellationToken.None);
        var removal = await _delete.Handle(new DeleteMySeniorRecordCommand(mine.MemberSeniorRecordKey), CancellationToken.None);

        change.Type.Should().Be(ResultType.Success);
        mine.Number.Should().Be("ЛШ 42");
        mine.Note.Should().BeNull();
        removal.Type.Should().Be(ResultType.Success);
        _records.Verify(r => r.Delete(mine, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnAccountWithNoPersonBehindIt_WritesNothing()
    {
        _members.Setup(m => m.GetSummaryByUserKeyAsync(_userKey, It.IsAny<CancellationToken>())).ReturnsAsync((MemberSummary?)null);

        (await Add("lsh-absolvent")).Type.Should().Be(ResultType.Forbidden);
        _stored.Should().BeEmpty();
    }

    [Fact]
    public void ADateInTheFuture_IsRefused()
    {
        var validator = new UpsertMySeniorRecordCommandValidator(new FixedTimeProvider(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero)));

        validator.Validate(new UpsertMySeniorRecordCommand { CatalogId = "lsh-absolvent", AchievedOn = new DateOnly(2026, 12, 1) })
            .IsValid.Should().BeFalse();
        validator.Validate(new UpsertMySeniorRecordCommand { CatalogId = "lsh-absolvent", AchievedOn = new DateOnly(2026, 11, 2) })
            .IsValid.Should().BeTrue();
    }
}
