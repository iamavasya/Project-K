using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KraiModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.KurinModule.Tags;
using ProjectK.Common.Models.Tags;
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
        var ordenMembers = new List<Member>();
        foreach (var (first, last, login, year) in OrdenMembers)
        {
            ordenMembers.Add(await EnsureSeniorMemberAsync(orden.KurinKey, first, last, $"orden.{login}@projectk.com", year, cancellationToken));
        }

        // The Order calls its писар «Скриптор»: same role, same rights, its own word.
        orden.RoleNames = KurinRoleNames.Normalize(new Dictionary<string, string?> { [nameof(LeadershipRole.Pysar)] = "Скриптор" }, out _);
        var ordenLeadership = await EnsureLeadershipAsync(LeadershipType.Kurin, orden.KurinKey, null, new DateOnly(2025, 9, 1), cancellationToken);
        SeatOffice(ordenLeadership, ordenMembers[1].MemberKey, LeadershipRole.Pysar, new DateOnly(2025, 9, 1));
        await SeedOrdenCardsAsync(orden.KurinKey, ordenMembers, cancellationToken);

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

    /// <summary>
    /// What a senior card shows besides the kurin and the role: tags (on in senior kurins by default)
    /// and a пересторога still running beside one that has run out, so the card has both kinds.
    /// </summary>
    private async Task SeedOrdenCardsAsync(Guid kurinKey, IReadOnlyList<Member> members, CancellationToken cancellationToken)
    {
        if (await _dbContext.KurinTags.AnyAsync(t => t.KurinKey == kurinKey, cancellationToken))
        {
            return;
        }

        var palette = KurinTagRules.Palette;
        var mountains = new KurinTag { KurinKey = kurinKey, Name = "Мандрівники", ColorHex = palette[0] };
        var council = new KurinTag { KurinKey = kurinKey, Name = "Рада куреня", ColorHex = palette[2] };
        var seniors = new KurinTag { KurinKey = kurinKey, Name = "Сеніори", ColorHex = palette[4] };
        _dbContext.KurinTags.AddRange(mountains, council, seniors);
        foreach (var (member, tag) in new[] { (members[0], mountains), (members[1], council), (members[1], mountains), (members[2], seniors), (members[3], seniors) })
        {
            _dbContext.MemberTags.Add(new MemberTag { KurinTagKey = tag.KurinTagKey, MemberKey = member.MemberKey });
        }

        // Their ступені: старші пластуни, двоє старших уже сеніори.
        var levels = new[] { PlastLevel.Starshoplastun, PlastLevel.Starshoplastun, PlastLevel.Senior, PlastLevel.SeniorPratsi, PlastLevel.Starshoplastun };
        for (var i = 0; i < members.Count; i++)
        {
            members[i].LatestPlastLevel = levels[i % levels.Length];
            _dbContext.PlastLevelHistories.Add(new PlastLevelHistory
            {
                MemberKey = members[i].MemberKey,
                KurinKey = kurinKey,
                PlastLevel = levels[i % levels.Length],
                DateAchieved = DateOnly.FromDateTime(_now.AddYears(-2 - i))
            });
        }

        var issuer = await _dbContext.Users.Where(u => u.Email == OrdenEmail).Select(u => u.Id).FirstAsync(cancellationToken);
        _dbContext.MemberWarnings.AddRange(
            new MemberWarning
            {
                MemberKey = members[0].MemberKey,
                KurinKey = kurinKey,
                Level = MemberWarningLevel.Level1,
                IssuedAtUtc = _now.AddDays(-12),
                ExpiresAtUtc = _now.AddDays(78),
                IssuedByUserKey = issuer
            },
            new MemberWarning
            {
                MemberKey = members[0].MemberKey,
                KurinKey = kurinKey,
                Level = MemberWarningLevel.Level1,
                IssuedAtUtc = _now.AddMonths(-9),
                ExpiresAtUtc = _now.AddMonths(-6),
                IssuedByUserKey = issuer
            });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    internal const string LifeStoryEmail = "orden.kozak@projectk.com";

    /// <summary>
    /// A senior of the Order who grew up in kurin 1: «Ведмеді» from 2015, the курінний провід's
    /// писар, two probes, вмілості and an УПЮ відзначення, then старший пластун and the Order. His
    /// card shows none of that; his «Пластовий життєпис» shows all of it.
    /// </summary>
    private async Task SeedLifeStoryAsync(
        Guid youthKurinKey,
        Group vedmedi,
        Leadership youthKurinLeadership,
        Person mentor,
        Person zvyazkovyi,
        CancellationToken cancellationToken)
    {
        var orden = await _dbContext.Kurins.FirstAsync(k => k.NamedAfter == "Орден Хрестоносців", cancellationToken);
        if (await _dbContext.Users.AnyAsync(u => u.Email == LifeStoryEmail, cancellationToken))
        {
            return;
        }

        var joined = new DateTime(2015, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var member = await DataSeeder.EnsureMemberAsync(
            _dbContext,
            _userManager,
            new DataSeeder.SeededPerson(LifeStoryEmail, "Андрій", "Тарасович", "Козак", youthKurinKey, vedmedi.GroupKey, NextPhone(), new DateOnly(2004, 5, 14), MembershipKind.Youth, joined),
            cancellationToken);

        var youth = await _dbContext.Memberships.FirstAsync(ms => ms.MemberKey == member.MemberKey && ms.KurinKey == youthKurinKey, cancellationToken);
        youth.LeftAtUtc = new DateTime(2022, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        _dbContext.Memberships.Add(new Membership
        {
            MemberKey = member.MemberKey,
            UserKey = member.UserKey,
            KurinKey = orden.KurinKey,
            Kind = MembershipKind.Youth,
            JoinedAtUtc = new DateTime(2022, 9, 15, 0, 0, 0, DateTimeKind.Utc)
        });

        foreach (var (level, when, kurinKey) in new (PlastLevel, DateOnly, Guid?)[]
                 {
                     (PlastLevel.Entry, new DateOnly(2015, 9, 1), youthKurinKey),
                     (PlastLevel.Uchasnyk, new DateOnly(2016, 10, 8), youthKurinKey),
                     (PlastLevel.Rozviduvach, new DateOnly(2018, 6, 2), youthKurinKey),
                     (PlastLevel.Skob, new DateOnly(2020, 7, 18), youthKurinKey),
                     (PlastLevel.Starshoplastun, new DateOnly(2022, 10, 1), orden.KurinKey)
                 })
        {
            _dbContext.PlastLevelHistories.Add(new PlastLevelHistory { MemberKey = member.MemberKey, KurinKey = kurinKey, PlastLevel = level, DateAchieved = when });
        }

        member.LatestPlastLevel = PlastLevel.Starshoplastun;
        SeatOffice(youthKurinLeadership, member.MemberKey, LeadershipRole.Pysar, new DateOnly(2020, 9, 1), new DateOnly(2022, 6, 30));

        var person = new Person(member, vedmedi.GroupKey, joined, 22) { Level = PlastLevel.Skob };
        var probes = _catalog.GetProbes();
        if (probes.Count > 0)
        {
            SeedProbesFor(person, probes, mentor, zvyazkovyi, youthKurinKey);
        }

        var badgeIds = _catalog.GetBadgeIds();
        if (badgeIds.Count > 0)
        {
            SeedBadgesFor(person, badgeIds, mentor, youthKurinKey, awaitingReview: false, refused: false);
        }

        var camp = new DateTime(2019, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        _dbContext.MemberAwards.Add(new MemberAward
        {
            MemberKey = member.MemberKey,
            KurinKey = youthKurinKey,
            Level = MemberAwardLevel.First,
            DateAcquired = camp,
            Note = "За організацію гурткової мандрівки",
            Status = BadgeProgressStatus.Confirmed,
            SubmittedAtUtc = camp.AddDays(3),
            SubmittedByUserKey = mentor.UserKey,
            ReviewedAtUtc = camp.AddDays(5),
            ReviewedByUserKey = zvyazkovyi.UserKey
        });

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

    private async Task<Member> EnsureSeniorMemberAsync(Guid kurinKey, string firstName, string lastName, string email, int birthYear, CancellationToken cancellationToken)
    {
        return await DataSeeder.EnsureMemberAsync(
            _dbContext,
            _userManager,
            new DataSeeder.SeededPerson(email, firstName, null, lastName, kurinKey, null, NextPhone(), new DateOnly(birthYear, 2, 3), MembershipKind.Youth, _now.AddYears(-4)),
            cancellationToken);
    }
}
