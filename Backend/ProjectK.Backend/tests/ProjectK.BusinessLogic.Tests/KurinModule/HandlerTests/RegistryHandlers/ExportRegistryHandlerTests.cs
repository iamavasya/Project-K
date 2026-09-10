using FluentAssertions;
using MediatR;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Registry.Export;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.RegistryHandlers
{
    /// <summary>
    /// The реєстр as a file. Asserted on the tables handed to the writer rather than on the bytes:
    /// what belongs in the реєстр is this handler's decision, and what an .xlsx looks like is not.
    /// </summary>
    public class ExportRegistryHandlerTests
    {
        private readonly Mock<IMediator> _mediator = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IKurinRepository> _kurins = new();
        private readonly Mock<ISpreadsheetWriter> _writer = new();
        private readonly ExportRegistryHandler _handler;

        private readonly Guid _kurinKey = Guid.NewGuid();
        private IReadOnlyList<SheetTable> _written = [];

        public ExportRegistryHandlerTests()
        {
            _uow.Setup(u => u.Kurins).Returns(_kurins.Object);
            KurinOfBranch(KurinBranch.UPYu);

            _writer
                .Setup(w => w.Write(It.IsAny<IReadOnlyList<SheetTable>>()))
                .Callback<IReadOnlyList<SheetTable>>(sheets => _written = sheets)
                .Returns([1, 2, 3]);

            _handler = new ExportRegistryHandler(
                _mediator.Object,
                _uow.Object,
                _writer.Object,
                TimeProvider.System);
        }

        private void KurinOfBranch(KurinBranch branch)
            => _kurins
                .Setup(k => k.GetByKeyAsync(_kurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Kurin(3) { KurinKey = _kurinKey, Branch = branch });

        private static MemberResponse Person(
            string lastName,
            PlastLevel? level,
            bool isStaff = false,
            string? groupName = null,
            params string[] mentored)
            => new()
            {
                MemberKey = Guid.NewGuid(),
                FirstName = "Тест",
                LastName = lastName,
                MiddleName = string.Empty,
                Email = "a@b.c",
                PhoneNumber = "0500000000",
                LatestPlastLevel = level,
                GroupName = groupName,
                IsStaff = isStaff,
                MentoredGroupNames = mentored,
                PlastLevelHistories = level is null
                    ? []
                    : [new PlastLevelHistoryDto { PlastLevel = level.Value, DateAchieved = new DateOnly(2024, 5, 1) }]
            };

        private void RosterIs(params MemberResponse[] people)
            => _mediator
                .Setup(m => m.Send(It.IsAny<GetMembers>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResult<IEnumerable<MemberResponse>>(ResultType.Success, people));

        private Task<ServiceResult<RegistryFile>> Export(params string[] columnIds)
            => _handler.Handle(new ExportRegistry(_kurinKey, columnIds), CancellationToken.None);

        private Dictionary<string, int?> Tally()
            => _written
                .Single(sheet => sheet.Name == "Чисельність")
                .Rows
                .ToDictionary(row => row[0].Text!, row => row[1].Number);

        [Fact]
        public async Task ShouldWriteYouthAndStaffToTheirOwnSheets()
        {
            RosterIs(
                Person("Юнак", PlastLevel.Uchasnyk, groupName: "Alpha"),
                Person("Виховна", PlastLevel.Starshoplastun, isStaff: true, mentored: "Alpha"));

            await Export("groupName");

            var youth = _written.Single(sheet => sheet.Name == "Юнаки");
            var staff = _written.Single(sheet => sheet.Name == "Впорядники");

            youth.Rows.Should().ContainSingle();
            youth.Rows[0][0].Text.Should().Be("Юнак Тест");
            staff.Rows.Should().ContainSingle();
            staff.Rows[0][0].Text.Should().Be("Виховна Тест");
        }

        /// <summary>
        /// A виховник's own гурток is nearly always empty — they are placed in the kurin, not in a
        /// гурток — so that column is swapped for the one that answers the question actually being
        /// asked, and it is there whether or not the screen had it turned on.
        /// </summary>
        [Fact]
        public async Task StaffSheet_ShouldLeadWithTheGroupsTheyMentor_EvenWhenTheColumnWasNotChosen()
        {
            RosterIs(Person("Виховна", PlastLevel.Starshoplastun, true, null, "Alpha", "Beta"));

            await Export("groupName", "phoneNumber");

            var staff = _written.Single(sheet => sheet.Name == "Впорядники");

            staff.Headers.Should().Equal("Прізвище та ім'я", "Гурток (закріплення)", "Телефон");
            staff.Rows[0][1].Text.Should().Be("Alpha, Beta");
        }

        [Fact]
        public async Task Tally_ShouldCountYouthByLevel_AndLeaveStaffOut()
        {
            RosterIs(
                Person("Один", PlastLevel.Uchasnyk),
                Person("Два", PlastLevel.Uchasnyk),
                Person("Три", PlastLevel.Skob),
                Person("Виховна", PlastLevel.Uchasnyk, isStaff: true));

            await Export();

            var counts = Tally();

            counts["пл. уч."].Should().Be(2, "the виховник is кадра, not юнацтво");
            counts["пл. скоб / вірл."].Should().Be(1);
            counts["Разом"].Should().Be(3);
        }

        /// <summary>
        /// Someone with no ступінь recorded still has to appear somewhere, or the column stops adding
        /// up to the roster — and a missing ступінь is the very thing this table is read to find.
        /// </summary>
        [Fact]
        public async Task Tally_ShouldNotSwallowPeopleWithNoLevel()
        {
            RosterIs(Person("Безступеневий", level: null), Person("Учасник", PlastLevel.Uchasnyk));

            await Export();

            var counts = Tally();

            counts["Без ступеня / інший"].Should().Be(1);
            counts["Разом"].Should().Be(2);
        }

        /// <summary>
        /// A УПЮ kurin has no use for сеньйорські rows and a УПС kurin does. The tally follows the
        /// same ladder the columns do rather than a list of its own.
        /// </summary>
        [Fact]
        public async Task Tally_ShouldFollowTheKurinsBranch()
        {
            KurinOfBranch(KurinBranch.UPS);
            RosterIs(Person("Сеньйор", PlastLevel.SeniorPratsi));

            await Export();

            var counts = Tally();

            counts.Should().ContainKey("пл. сен. праці");
            counts["пл. сен. праці"].Should().Be(1);
            counts.Should().NotContainKey("Без ступеня / інший");
        }
    }
}
