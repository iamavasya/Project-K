using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KraiModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.Infrastructure.Seeding;

/// <summary>
/// Senior kurins beside the УПЮ one: Орден Хрестоносців, one kurin in two гілки (5-й УСП and 20-й
/// УПС), a УСП kurin waiting on its request to join УПС too, and a senior kurin's application on the
/// waitlist. Their Зв'язкові have fixed addresses so the demo can name them:
/// <c>orden@projectk.com</c> and <c>chorty@projectk.com</c>.
/// </summary>
public partial class DemoDataSeeder
{
    internal const string OrdenEmail = "orden@projectk.com";
    internal const string ChortyEmail = "chorty@projectk.com";

    private async Task SeedSeniorKurinsAsync(CancellationToken cancellationToken)
    {
        var orden = await EnsureSeniorKurinAsync(
            [new KurinNumbering(KurinBranch.USP, 5), new KurinNumbering(KurinBranch.UPS, 20)],
            "Орден Хрестоносців",
            "Львів",
            "Старший курінь у двох уладах: 5-й курінь УСП і 20-й курінь УПС. Один склад, одна домівка, "
            + "сходини щочетверга й мандрівки Карпатами.",
            cancellationToken);
        await EnsureSeniorLeaderAsync(orden.KurinKey, OrdenEmail, "Остап", "Гнатюк", 1979, cancellationToken);
        foreach (var (first, last, login, year) in OrdenMembers)
        {
            await EnsureSeniorMemberAsync(orden.KurinKey, first, last, $"orden.{login}@projectk.com", year, cancellationToken);
        }

        var chorty = await EnsureSeniorKurinAsync(
            [new KurinNumbering(KurinBranch.USP, 7)],
            "Лісові Чорти",
            "Тернопіль",
            "Курінь УСП. Від минулого року частина куреня вже сеньйори, тож курінь просить і числа в УПС.",
            cancellationToken);
        var chortyLink = await EnsureSeniorLeaderAsync(chorty.KurinKey, ChortyEmail, "Марта", "Ковалишин", 1984, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!await _dbContext.KurinBranchRequests.AnyAsync(r => r.KurinKey == chorty.KurinKey, cancellationToken))
        {
            _dbContext.KurinBranchRequests.Add(new KurinBranchRequest
            {
                KurinKey = chorty.KurinKey,
                RequestedByUserKey = chortyLink.UserKey!.Value,
                UspNumber = 7,
                UpsNumber = 12,
                Note = "Половина куреня вже сеньйори, а число 12 в УПС вільне — підтверджено з крайовою булавою.",
                RequestedAtUtc = _now.AddDays(-1)
            });
        }

        const string applicantEmail = "kulchytskyi@projectk.com";
        if (!await _dbContext.WaitlistEntries.AnyAsync(e => e.Email == applicantEmail, cancellationToken))
        {
            _dbContext.WaitlistEntries.Add(new WaitlistEntry
            {
                WaitlistEntryKey = Guid.NewGuid(),
                FirstName = "Ярема",
                LastName = "Кульчицький",
                Email = applicantEmail,
                PhoneNumber = "+380671234508",
                DateOfBirth = new DateTime(1981, 3, 14),
                Stanytsia = "Івано-Франківськ",
                KraiCode = Krai.Ukraine,
                IsKurinLeaderCandidate = true,
                ClaimedUspNumber = 9,
                ClaimedUpsNumber = 31,
                RequestedAtUtc = _now.AddHours(-5)
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static readonly (string First, string Last, string Login, int Year)[] OrdenMembers =
    [
        ("Богдан", "Лисяк", "lysiak", 1995),
        ("Ірина", "Федорів", "fedoriv", 1998),
        ("Тарас", "Мельничук", "melnychuk", 1972),
        ("Олена", "Ничай", "nychai", 1969),
        ("Андрій", "Савчин", "savchyn", 2001)
    ];

    private async Task<Kurin> EnsureSeniorKurinAsync(
        IReadOnlyCollection<KurinNumbering> branches,
        string name,
        string stanytsia,
        string description,
        CancellationToken cancellationToken)
    {
        var main = branches.First();
        var kurin = await _dbContext.Kurins.FirstOrDefaultAsync(
            k => k.KraiCode == Krai.Ukraine && k.Branches.Any(b => b.Branch == main.Branch && b.Number == main.Number),
            cancellationToken);
        if (kurin is null)
        {
            kurin = Kurin.InBranches(branches);
            _dbContext.Kurins.Add(kurin);
        }

        kurin.NamedAfter = name;
        kurin.Stanytsia = stanytsia;
        kurin.RegionOrCountry = "Україна";
        kurin.Description = description;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return kurin;
    }

    /// <summary>The kurin's Зв'язковий: a КВ office, which is where the rights in its settings come from.</summary>
    private async Task<Member> EnsureSeniorLeaderAsync(Guid kurinKey, string email, string firstName, string lastName, int birthYear, CancellationToken cancellationToken)
    {
        var member = await DataSeeder.EnsureMemberAsync(
            _dbContext,
            _userManager,
            new DataSeeder.SeededPerson(email, firstName, null, lastName, kurinKey, null, NextPhone(), new DateOnly(birthYear, 5, 12), MembershipKind.Staff, _now.AddYears(-6)),
            cancellationToken);

        var kv = await EnsureLeadershipAsync(LeadershipType.KV, kurinKey, null, new DateOnly(2023, 9, 1), cancellationToken);
        SeatOffice(kv, member.MemberKey, LeadershipRole.Zvyazkovyi, new DateOnly(2023, 9, 1));
        return member;
    }

    private async Task EnsureSeniorMemberAsync(Guid kurinKey, string firstName, string lastName, string email, int birthYear, CancellationToken cancellationToken)
    {
        await DataSeeder.EnsureMemberAsync(
            _dbContext,
            _userManager,
            new DataSeeder.SeededPerson(email, firstName, null, lastName, kurinKey, null, NextPhone(), new DateOnly(birthYear, 2, 3), MembershipKind.Youth, _now.AddYears(-4)),
            cancellationToken);
    }
}
