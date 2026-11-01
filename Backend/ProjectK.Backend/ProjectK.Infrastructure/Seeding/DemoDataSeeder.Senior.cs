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
        var ordenLeader = await EnsureSeniorLeaderAsync(orden.KurinKey, OrdenEmail, "Остап", "Гнатюк", 1979, cancellationToken);
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
        await SeedSeniorBooksAsync(ordenLeader, ordenMembers[1], cancellationToken);

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

    /// <summary>
    /// Книжечки the people keep themselves: the Order's зв'язковий with a few decades of them, and its
    /// Скриптор with her first. Ids are the catalogue's.
    /// </summary>
    private async Task SeedSeniorBooksAsync(Member leader, Member scribe, CancellationToken cancellationToken)
    {
        if (await _dbContext.MemberSeniorRecords.AnyAsync(r => r.MemberKey == leader.MemberKey, cancellationToken))
        {
            return;
        }

        static MemberSeniorRecord Line(Member who, string catalogId, DateOnly? on, string? number = null, string? note = null)
            => new() { MemberKey = who.MemberKey, CatalogId = catalogId, AchievedOn = on, Number = number, Note = note };

        _dbContext.MemberSeniorRecords.AddRange(
            Line(leader, "usp-starshyi-plastun", new DateOnly(1997, 11, 15)),
            Line(leader, "ups-senior-prykhylnyk", new DateOnly(2014, 4, 26)),
            Line(leader, "ups-senior-pratsi", new DateOnly(2016, 5, 14)),
            Line(leader, "lsh-absolvent", new DateOnly(1999, 8, 20), "ЛШ 41"),
            Line(leader, "kv-i-upyu", new DateOnly(2001, 7, 10), "КВ-І 118"),
            Line(leader, "usp-hramota-zasluhy", new DateOnly(2012, 10, 20), note: "За табір «Вогні Карпат»"),
            Line(leader, "orden-sv-yuriia-bronza", new DateOnly(2018, 4, 23)),
            Line(scribe, "usp-starsha-plastunka", new DateOnly(2016, 12, 3)),
            Line(scribe, "shb-i", new DateOnly(2018, 8, 1), "ШБ 22"),
            Line(scribe, "usp-hramota-podiaky", new DateOnly(2024, 6, 9)));
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
