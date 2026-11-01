using FluentAssertions;
using ProjectK.BusinessLogic.Modules.DuesModule.Services;
using ProjectK.Common.Entities.DuesModule;
using ProjectK.Common.Models.Dues;
using ProjectK.Common.Models.Enums;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.DuesModule;

/// <summary>
/// A senior kurin's вкладка: no гуртки, each person's account in the kurin's own box, rates by tag,
/// and payees the kurin names itself. The УСП rate is 300 to «Булава УСП» (200 with a пільга) and 150
/// to the kurin (100); the rate for everyone else is 400 to the kurin.
/// </summary>
public class SeniorDuesLedgerTests
{
    private static readonly Guid Kurin = Guid.NewGuid();
    private static readonly Guid Usp = Guid.NewGuid();
    private static readonly Guid Bulava = Guid.NewGuid();
    private static readonly Guid Person = Guid.NewGuid();

    private static readonly DuesQuarter Q1 = new(2026, 1);
    private static readonly DuesQuarter Q2 = new(2026, 2);

    private readonly List<KurinDuesRate> _rates = [];
    private readonly List<DuesConcession> _concessions = [];
    private readonly List<DuesCharge> _charges = [];
    private readonly List<DuesEntry> _entries = [];

    public SeniorDuesLedgerTests()
    {
        var everyone = new KurinDuesRate { KurinKey = Kurin, FromQuarter = Q1.Index };
        everyone.SetArticles([new KurinDuesArticle { Name = "Членська", Recipient = DuesRecipient.Kurin, Amount = 400, ReducedAmount = 300 }]);
        var usp = new KurinDuesRate { KurinKey = Kurin, TagKey = Usp, FromQuarter = Q1.Index };
        usp.SetArticles(
        [
            new KurinDuesArticle { Name = "Вкладка УСП", Recipient = DuesRecipient.Payee, PayeeKey = Bulava, Amount = 300, ReducedAmount = 200 },
            new KurinDuesArticle { Name = "Курінна", Recipient = DuesRecipient.Kurin, Amount = 150, ReducedAmount = 100 }
        ]);
        _rates.AddRange([everyone, usp]);
    }

    private DuesLedger Ledger() => new(_rates, [], _concessions, _charges, _entries);

    private void Charge(Guid? tag, params DuesQuarter[] quarters) =>
        _charges.AddRange(quarters.Select(q => new DuesCharge { KurinKey = Kurin, MembershipKey = Person, Quarter = q.Index, RateTagKey = tag }));

    private void Entry(DuesEntryKind kind, decimal amount, Guid? membership = null, Guid? payee = null) =>
        _entries.Add(new DuesEntry
        {
            KurinKey = Kurin,
            MembershipKey = membership,
            PayeeKey = payee,
            Kind = kind,
            Amount = amount,
            Method = DuesPaymentMethod.Card,
            OccurredOn = new DateOnly(2026, 2, 1)
        });

    [Fact]
    public void AQuarter_IsPricedByTheTagItWasChargedAt()
    {
        Charge(Usp, Q1);
        Charge(null, Q2);

        var account = Ledger().Accounts().Single();

        account.GroupKey.Should().BeNull();
        account.Quarters.Select(q => q.Charged.Total).Should().Equal(450, 400);
        account.Quarters[0].Charged.Payees.Of(Bulava).Should().Be(300);
    }

    [Fact]
    public void APayment_GoesToThePayeeBeforeTheKurin()
    {
        Charge(Usp, Q1);
        Entry(DuesEntryKind.Contribution, 350, Person);

        var quarter = Ledger().Accounts().Single().Quarters.Single();

        quarter.Paid.Payees.Of(Bulava).Should().Be(300);
        quarter.Paid.Kurin.Should().Be(50);
        quarter.Balance.Should().Be(-100);
    }

    [Fact]
    public void TheKurinsPilha_TakesTheReducedAmountsOfEveryStattia()
    {
        Charge(Usp, Q1);
        _concessions.Add(new DuesConcession { KurinKey = Kurin, MembershipKey = Person, FromQuarter = Q1.Index, IsConcession = true });

        var charged = Ledger().Accounts().Single().Quarters.Single().Charged;

        charged.Payees.Of(Bulava).Should().Be(200);
        charged.Kurin.Should().Be(100);
    }

    [Fact]
    public void PaidAhead_TheSurplusPaysTheNextQuarterWhenItIsCharged()
    {
        Charge(Usp, Q1);
        Entry(DuesEntryKind.Contribution, 1800, Person);

        Ledger().Accounts().Single().Balance.Should().Be(1350);
        Ledger().KurinBox().Box.Ahead.Should().Be(1350);

        Charge(Usp, Q2);
        var account = Ledger().Accounts().Single();
        account.Quarters.Should().OnlyContain(q => q.Balance == 0);
        account.Balance.Should().Be(900);
    }

    [Fact]
    public void TheKurinsBox_OwesThePayeeWhatWasPaidForIt_LessWhatWasSent()
    {
        Charge(Usp, Q1, Q2);
        Entry(DuesEntryKind.Contribution, 900, Person);
        Entry(DuesEntryKind.TransferToPayee, 250, payee: Bulava);

        var box = Ledger().KurinBox();

        box.Box.Total.Should().Be(650);
        box.Payees.Should().ContainSingle().Which.Should().Be(new PayeeHandover(Bulava, 600, 250));
        box.Box.ToForward.Should().Be(350);
        box.Box.Ahead.Should().Be(0);
        box.Box.Own.Should().Be(300);
        box.Handovers.Should().BeEmpty();
    }

    [Fact]
    public void DebtForPastQuarters_IsReadFromTheKurinsOwnAccount()
    {
        Charge(Usp, Q1, Q2);
        Entry(DuesEntryKind.Contribution, 450, Person);

        var owed = Ledger().Accounts()
            .SelectMany(a => a.Quarters)
            .Where(q => q.Quarter < Q2 && q.Balance < 0)
            .Sum(q => -q.Balance);

        owed.Should().Be(0);
        Ledger().Accounts().Single().Balance.Should().Be(-450);
    }

    [Fact]
    public void RateTagFor_PicksTheHighestRateInForce_OrNoneWithoutATaggedRate()
    {
        var ups = Guid.NewGuid();
        var upsRate = new KurinDuesRate { KurinKey = Kurin, TagKey = ups, FromQuarter = Q2.Index, KurinShare = 500 };
        var rates = _rates.Append(upsRate).ToList();

        DuesLedger.RateTagFor(rates, [Usp, ups], Q1).Should().Be(Usp);
        DuesLedger.RateTagFor(rates, [Usp, ups], Q2).Should().Be(ups);
        DuesLedger.RateTagFor(rates, [Guid.NewGuid()], Q2).Should().BeNull();
        DuesLedger.RateTagFor(rates, [], Q2).Should().BeNull();
    }

    [Fact]
    public void APilhaOfTheKraiOnTheKraisPart_LowersTheShareOfAKraiPayee()
    {
        Charge(Usp, Q1);
        var grant = new DuesConcessionGrant
        {
            KurinKey = Kurin, MembershipKey = Person, ConcessionKey = "large", Name = "Багатодітна родина",
            Kind = ProjectK.Common.Models.KraiProfiles.KraiDues.Exempt, Parts = DuesParts.Krai, FromQuarter = Q1.Index
        };

        var withBulavaOfTheKrai = new DuesLedger(_rates, [], _concessions, _charges, _entries, [grant], [Bulava]).Accounts().Single();
        var withBulavaApart = new DuesLedger(_rates, [], _concessions, _charges, _entries, [grant]).Accounts().Single();

        withBulavaOfTheKrai.Quarters.Single().Charged.Total.Should().Be(150);
        withBulavaOfTheKrai.Quarters.Single().ConcessionName.Should().Be("Багатодітна родина");
        withBulavaApart.Quarters.Single().Charged.Total.Should().Be(450);
    }
}
