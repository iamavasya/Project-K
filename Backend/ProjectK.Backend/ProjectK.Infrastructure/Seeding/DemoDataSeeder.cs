using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Entities.KurinModule.Planning;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Seeding;

/// <summary>
/// Seeds a kurin that looks lived-in: three гуртки with their провід and виховники, a КВ, people
/// with по батькові, addresses, schools and ступені reached at believable dates, probes signed
/// point by point over months, вмілості confirmed and a few still waiting for review, відзначення,
/// перестороги, a calendar of сходини, табори and заходи with RSVPs, a task board, and camp
/// planning — so every screen has something on it the moment the stack starts.
/// <para>
/// Randomness is seeded, so two runs produce the same kurin: screenshots, fixtures and a colleague's
/// machine all show the same people. Dates are relative to now, so the calendar always has a past
/// and a future. The first account created is the Зв'язковий, which keeps <c>demo0</c> meaning what
/// the docs and the screenshot script say it means.
/// </para>
/// </summary>
public class DemoDataSeeder : IDemoDataSeeder
{
    private const int KurinNumber = 1;

    private static readonly string[] FirstNames =
    {
        "Андрій", "Богдан", "Василь", "Григорій", "Дмитро", "Остап", "Ігор", "Тарас",
        "Юрій", "Роман", "Степан", "Микола", "Олег", "Павло", "Сергій", "Назар",
        "Максим", "Орест", "Левко", "Артем", "Данило", "Марко", "Захар", "Устим",
        "Ярослав", "Мирослав", "Святослав", "Володимир", "Любомир", "Ростислав",
        "Матвій", "Тимофій", "Іван", "Михайло", "Олексій", "Віталій"
    };

    private static readonly string[] MiddleNames =
    {
        "Андрійович", "Богданович", "Васильович", "Ігорович", "Тарасович", "Юрійович",
        "Романович", "Степанович", "Миколайович", "Олегович", "Павлович", "Сергійович",
        "Назарович", "Орестович", "Ярославович", "Мирославович", "Володимирович",
        "Любомирович", "Петрович", "Іванович", "Михайлович", "Олександрович", "Зіновійович"
    };

    private static readonly string[] LastNames =
    {
        "Шевченко", "Франко", "Коваль", "Бондаренко", "Мельник", "Ткаченко", "Кравчук", "Гнатюк",
        "Панчук", "Савчук", "Романюк", "Дідух", "Іваненко", "Кузьменко", "Лисенко", "Марчук",
        "Гаврилюк", "Оліярник", "Соловей", "Вербицький", "Гончар", "Пасічник", "Цимбалюк", "Яремчук",
        "Стельмах", "Чорновіл", "Кушнір", "Бойчук", "Сорока", "Левицький",
        "Кміть", "Кропива", "Мартинюк", "Заяць", "Гуцул", "Пилипчук"
    };

    // A diaspora kurin: the станиця is Luxembourg, so the people live on its streets and go to
    // its schools, while the numbers they are reached at stay Ukrainian — that is the only mask
    // the member form knows.
    private static readonly string[] Streets =
    {
        "rue de la Gare", "avenue de la Liberté", "boulevard Royal", "rue Notre-Dame", "rue des Bains",
        "route d'Esch", "rue de Hollerich", "avenue Pasteur", "rue Adolphe Fischer", "rue de Strasbourg",
        "rue du Fort Neipperg", "avenue de la Faïencerie", "rue de Bonnevoie"
    };

    private static readonly string[] Postcodes =
    {
        "1611", "1930", "2449", "2240", "1212", "1470", "1741", "2310", "1520", "2560", "2230", "1510", "1260"
    };

    private static readonly string[] Schools =
    {
        "Athénée de Luxembourg", "Lycée Michel-Rodange", "Lycée de Garçons de Luxembourg",
        "Lycée Aline Mayrisch", "École européenne Luxembourg I", "École européenne Luxembourg II",
        "Lycée Robert-Schuman", "International School of Luxembourg", "Lycée Vauban",
        "Lycée Technique du Centre"
    };

    private static readonly string[] MobileCodes =
    {
        "67", "68", "96", "97", "98", "50", "66", "95", "99", "63", "73", "93"
    };

    private static readonly string[] BadgeConfirmNotes =
    {
        "Показав на сходинах", "Здав на таборі", "Перевірив на мандрівці", "Підтверджено на вишколі", null!
    };

    // Taken from the office registry rather than restated: the local copy had already lost
    // Hronikar from the гуртковий провід, so demo data no longer matched what the app allows.
    // The «інша роль» placeholders are left empty: a seat with no name is not a провід anyone has.
    private static readonly LeadershipRole[] GroupOffices =
        LeadershipOffices.Grouping[LeadershipType.Group]
            .Where(role => role != LeadershipRole.OtherGroup)
            .ToArray();

    private static readonly LeadershipRole[] KurinOffices =
        LeadershipOffices.Grouping[LeadershipType.Kurin]
            .Where(role => role != LeadershipRole.OtherKurin)
            .ToArray();

    private static readonly TimeZoneInfo Kyiv = ResolveKyiv();

    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly IProgressCatalogReader _catalog;

    private readonly Random _random = new(20260920);
    private readonly DateTime _now = DateTime.UtcNow;
    private readonly HashSet<string> _phones = new();
    private readonly string[] _firstNames;
    private readonly string[] _lastNames;

    private int _personIndex;
    private int _emailIndex;

    public DemoDataSeeder(AppDbContext dbContext, UserManager<AppUser> userManager, IProgressCatalogReader catalog)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _catalog = catalog;
        _firstNames = Shuffled(FirstNames);
        _lastNames = Shuffled(LastNames);
    }

    /// <summary>One seeded person, kept in memory for the passes that reference each other.</summary>
    private sealed record Person(Member Member, Guid? GroupKey, DateTime JoinedAtUtc, int Age)
    {
        public Guid Key => Member.MemberKey;
        public Guid UserKey => Member.UserKey!.Value;
        public string FullName => $"{Member.FirstName} {Member.LastName}";
        public PlastLevel? Level { get; set; }
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var kurin = await EnsureKurinAsync(cancellationToken);
        var kurinKey = kurin.KurinKey;

        var sokoly = await DataSeeder.EnsureGroupAsync(_dbContext, "Соколи", kurinKey, cancellationToken);
        var levy = await DataSeeder.EnsureGroupAsync(_dbContext, "Леви", kurinKey, cancellationToken);
        var vedmedi = await DataSeeder.EnsureGroupAsync(_dbContext, "Ведмеді", kurinKey, cancellationToken);
        sokoly.Description = "Старші юнаки, 14–16 років. Сходини щосереди о 17:00 у домівці.";
        levy.Description = "Молодші юнаки, 12–14 років. Сходини щопʼятниці о 16:00.";
        vedmedi.Description = "Провідний гурток: з його складу обирається курінний провід.";

        var kvLeadership = await EnsureLeadershipAsync(LeadershipType.KV, kurinKey, null, new DateOnly(2024, 9, 1), cancellationToken);

        // 1. Зв'язковий first: demo0 is the account the docs and the screenshot script sign in with.
        var zvyazkovyi = await CreateStaffAsync(kurinKey, birthYear: 1986, joinedYearsAgo: 2, cancellationToken);
        SeatOffice(kvLeadership, zvyazkovyi.Key, LeadershipRole.Zvyazkovyi, new DateOnly(2024, 9, 1));

        // 2. Ordinary гуртки: the first six hold the гуртковий провід since last September.
        var sokolyYouth = await CreateYouthAsync(kurinKey, sokoly.GroupKey, 9, minAge: 14, maxAge: 16, minYearsInPlast: 2, maxYearsInPlast: 5, cancellationToken);
        var levyYouth = await CreateYouthAsync(kurinKey, levy.GroupKey, 8, minAge: 12, maxAge: 14, minYearsInPlast: 1, maxYearsInPlast: 3, cancellationToken);

        var sokolyLeadership = await EnsureLeadershipAsync(LeadershipType.Group, null, sokoly.GroupKey, new DateOnly(2025, 9, 1), cancellationToken);
        var levyLeadership = await EnsureLeadershipAsync(LeadershipType.Group, null, levy.GroupKey, new DateOnly(2025, 9, 1), cancellationToken);
        SeatOffices(sokolyLeadership, sokolyYouth, GroupOffices, new DateOnly(2025, 9, 1));
        SeatOffices(levyLeadership, levyYouth, GroupOffices, new DateOnly(2025, 9, 1));

        // Last year's гуртковий of Соколи handed over: a closed row is what the history table shows.
        SeatOffice(sokolyLeadership, sokolyYouth[6].Key, LeadershipRole.Hurtkoviy, new DateOnly(2024, 9, 1), new DateOnly(2025, 8, 31));

        // 3. Провідний гурток "Ведмеді": its members form the курінний провід.
        var vedmediYouth = await CreateYouthAsync(kurinKey, vedmedi.GroupKey, 8, minAge: 15, maxAge: 17, minYearsInPlast: 3, maxYearsInPlast: 5, cancellationToken);
        var kurinLeadership = await EnsureLeadershipAsync(LeadershipType.Kurin, kurinKey, null, new DateOnly(2025, 9, 1), cancellationToken);
        var kurinOfficeHolders = SeatOffices(kurinLeadership, vedmediYouth, KurinOffices, new DateOnly(2025, 9, 1));

        // 4. One Впорядник per гурток: a КВ office plus the mentor assignment that scopes their access.
        var mentors = new Dictionary<Guid, Person>();
        foreach (var (group, sinceYears) in new[] { (sokoly, 3), (levy, 1), (vedmedi, 4) })
        {
            var mentor = await CreateStaffAsync(kurinKey, birthYear: _random.Next(1994, 2002), joinedYearsAgo: sinceYears, cancellationToken);
            SeatOffice(kvLeadership, mentor.Key, LeadershipRole.Vykhovnyk, YearsAgo(sinceYears));
            _dbContext.MentorAssignments.Add(new MentorAssignment
            {
                MentorUserKey = mentor.UserKey,
                GroupKey = group.GroupKey,
                AssignedAtUtc = _now.AddYears(-sinceYears).AddDays(-_random.Next(0, 30))
            });
            mentors[group.GroupKey] = mentor;
        }

        var instructor = await CreateStaffAsync(kurinKey, birthYear: 1990, joinedYearsAgo: 2, cancellationToken);
        SeatOffice(kvLeadership, instructor.Key, LeadershipRole.Instruktor, YearsAgo(2));

        // 5. Two people who left: the реєстр has a «Колишні» section for a reason.
        await CreateFormerAsync(kurinKey, sokoly.GroupKey, sokolyLeadership, LeadershipRole.Pysar, leftMonthsAgo: 4, cancellationToken);
        await CreateFormerAsync(kurinKey, levy.GroupKey, null, null, leftMonthsAgo: 11, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var youth = sokolyYouth.Concat(levyYouth).Concat(vedmediYouth).ToList();
        var staff = new List<Person> { zvyazkovyi, instructor };
        staff.AddRange(mentors.Values);

        SeedPlastLevels(youth, staff, kurinKey);
        SeedProfileVerification(youth, zvyazkovyi);
        SeedProgress(youth, mentors, zvyazkovyi, kurinKey);
        SeedAwards(youth, mentors, zvyazkovyi, kurinKey);
        SeedWarnings(youth, mentors, zvyazkovyi, kurinKey);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var groups = new Dictionary<string, Group> { ["Соколи"] = sokoly, ["Леви"] = levy, ["Ведмеді"] = vedmedi };
        var leaderships = new Dictionary<string, Leadership>
        {
            ["KV"] = kvLeadership,
            ["Kurin"] = kurinLeadership,
            ["Соколи"] = sokolyLeadership,
            ["Леви"] = levyLeadership
        };
        SeedAgenda(kurinKey, groups, leaderships, youth, mentors, zvyazkovyi, kurinOfficeHolders);
        SeedPlanning(kurinKey, youth, mentors, zvyazkovyi);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Kurin> EnsureKurinAsync(CancellationToken cancellationToken)
    {
        var kurin = await _dbContext.Kurins.FirstOrDefaultAsync(k => k.Number == KurinNumber, cancellationToken);
        if (kurin == null)
        {
            kurin = new Kurin(KurinNumber) { IsZbtKurin = true };
            _dbContext.Kurins.Add(kurin);
        }

        kurin.NamedAfter = "Івана Богуна";
        kurin.Stanytsia = "Люксембург";
        kurin.RegionOrCountry = "Люксембург";
        kurin.Branch = KurinBranch.UPYu;
        kurin.ProfileVerificationEnabled = true;
        kurin.Description =
            "Курінь УПЮ при станиці Люксембург. Три гуртки, сходини щосуботи в домівці в Бонневуа, "
            + "літній табір в Арденнах і зимовий — у Вогезах. Заснований 2022 року українською "
            + "громадою Люксембургу.";

        await _dbContext.SaveChangesAsync(cancellationToken);
        return kurin;
    }

    private async Task<Person> CreateStaffAsync(Guid kurinKey, int birthYear, int joinedYearsAgo, CancellationToken cancellationToken)
    {
        var dateOfBirth = new DateOnly(birthYear, _random.Next(1, 13), _random.Next(1, 28));
        var joined = _now.AddYears(-joinedYearsAgo).AddDays(-_random.Next(0, 120));
        var member = await DataSeeder.EnsureMemberAsync(
            _dbContext,
            _userManager,
            NextPerson(kurinKey, null, dateOfBirth, MembershipKind.Staff, joined, school: null),
            cancellationToken);

        return new Person(member, null, joined, AgeOf(dateOfBirth));
    }

    private async Task<List<Person>> CreateYouthAsync(
        Guid kurinKey,
        Guid groupKey,
        int count,
        int minAge,
        int maxAge,
        int minYearsInPlast,
        int maxYearsInPlast,
        CancellationToken cancellationToken)
    {
        var people = new List<Person>(count);
        for (var i = 0; i < count; i++)
        {
            var age = _random.Next(minAge, maxAge + 1);
            var dateOfBirth = DateOnly.FromDateTime(_now.AddYears(-age).AddDays(-_random.Next(0, 365)));
            var yearsInPlast = Math.Min(_random.Next(minYearsInPlast, maxYearsInPlast + 1), Math.Max(1, age - 10));
            var joined = _now.AddYears(-yearsInPlast).AddDays(-_random.Next(0, 200));

            var member = await DataSeeder.EnsureMemberAsync(
                _dbContext,
                _userManager,
                NextPerson(kurinKey, groupKey, dateOfBirth, MembershipKind.Youth, joined, Pick(Schools)),
                cancellationToken);

            people.Add(new Person(member, groupKey, joined, age));
        }

        return people;
    }

    private async Task CreateFormerAsync(
        Guid kurinKey,
        Guid groupKey,
        Leadership? leadership,
        LeadershipRole? formerOffice,
        int leftMonthsAgo,
        CancellationToken cancellationToken)
    {
        var age = _random.Next(15, 18);
        var dateOfBirth = DateOnly.FromDateTime(_now.AddYears(-age).AddDays(-_random.Next(0, 365)));
        var joined = _now.AddYears(-4).AddDays(-_random.Next(0, 200));
        var member = await DataSeeder.EnsureMemberAsync(
            _dbContext,
            _userManager,
            NextPerson(kurinKey, groupKey, dateOfBirth, MembershipKind.Youth, joined, Pick(Schools)),
            cancellationToken);

        var left = _now.AddMonths(-leftMonthsAgo);
        var membership = await _dbContext.Memberships
            .FirstAsync(ms => ms.MemberKey == member.MemberKey && ms.LeftAtUtc == null, cancellationToken);
        membership.LeftAtUtc = left;

        if (leadership is not null && formerOffice is not null)
        {
            SeatOffice(leadership, member.MemberKey, formerOffice.Value, new DateOnly(2024, 9, 1), DateOnly.FromDateTime(left));
        }

        _dbContext.PlastLevelHistories.Add(new PlastLevelHistory
        {
            MemberKey = member.MemberKey,
            KurinKey = kurinKey,
            PlastLevel = PlastLevel.Entry,
            DateAchieved = DateOnly.FromDateTime(joined)
        });
        _dbContext.PlastLevelHistories.Add(new PlastLevelHistory
        {
            MemberKey = member.MemberKey,
            KurinKey = kurinKey,
            PlastLevel = PlastLevel.Uchasnyk,
            DateAchieved = DateOnly.FromDateTime(joined.AddMonths(15))
        });
        member.LatestPlastLevel = PlastLevel.Uchasnyk;
    }

    private DataSeeder.SeededPerson NextPerson(
        Guid kurinKey,
        Guid? groupKey,
        DateOnly dateOfBirth,
        MembershipKind kind,
        DateTime joinedAtUtc,
        string? school)
    {
        var firstName = _firstNames[_personIndex % _firstNames.Length];
        var lastName = _lastNames[(_personIndex + _personIndex / _lastNames.Length) % _lastNames.Length];
        _personIndex++;

        return new DataSeeder.SeededPerson(
            $"demo{_emailIndex++}@projectk.com",
            firstName,
            Pick(MiddleNames),
            lastName,
            kurinKey,
            groupKey,
            NextPhone(),
            dateOfBirth,
            kind,
            joinedAtUtc,
            $"{_random.Next(1, 140)}, {Pick(Streets)}, L-{Pick(Postcodes)} Люксембург",
            school);
    }

    private string NextPhone()
    {
        while (true)
        {
            var phone = $"+380{Pick(MobileCodes)}{_random.Next(1000000, 9999999)}";
            if (_phones.Add(phone))
            {
                return phone;
            }
        }
    }

    // ── Ступені ──────────────────────────────────────────────────────────────────────────────────

    private void SeedPlastLevels(IEnumerable<Person> youth, IEnumerable<Person> staff, Guid kurinKey)
    {
        foreach (var person in youth)
        {
            var joined = person.JoinedAtUtc;
            var yearsIn = (_now - joined).TotalDays / 365.0;
            var ladder = new List<(PlastLevel Level, DateTime When)> { (PlastLevel.Entry, joined) };

            if (yearsIn >= 0.6)
            {
                ladder.Add((PlastLevel.Prykhylnyk, joined.AddMonths(6)));
            }

            if (yearsIn >= 1.4 && person.Age >= 12)
            {
                ladder.Add((PlastLevel.Uchasnyk, joined.AddMonths(15)));
            }

            if (yearsIn >= 3.1 && person.Age >= 14)
            {
                ladder.Add((PlastLevel.Rozviduvach, joined.AddMonths(36)));
            }

            if (yearsIn >= 4.6 && person.Age >= 16)
            {
                ladder.Add((PlastLevel.Skob, joined.AddMonths(54)));
            }

            foreach (var (level, when) in ladder)
            {
                _dbContext.PlastLevelHistories.Add(new PlastLevelHistory
                {
                    MemberKey = person.Key,
                    KurinKey = kurinKey,
                    PlastLevel = level,
                    DateAchieved = DateOnly.FromDateTime(when)
                });
            }

            person.Level = ladder[^1].Level;
            person.Member.LatestPlastLevel = person.Level;
        }

        // Staff earned theirs elsewhere, years ago: no kurin on those rows, which is what null means.
        foreach (var person in staff)
        {
            var eleven = person.Member.DateOfBirth.AddYears(11);
            var ladder = new List<(PlastLevel Level, DateOnly When)>
            {
                (PlastLevel.Entry, eleven),
                (PlastLevel.Uchasnyk, eleven.AddYears(1)),
                (PlastLevel.Rozviduvach, eleven.AddYears(3)),
                (PlastLevel.Skob, eleven.AddYears(5)),
                (PlastLevel.Starshoplastun, person.Member.DateOfBirth.AddYears(18))
            };

            if (person.Age >= 35)
            {
                ladder.Add((PlastLevel.Senior, person.Member.DateOfBirth.AddYears(35)));
            }

            foreach (var (level, when) in ladder)
            {
                _dbContext.PlastLevelHistories.Add(new PlastLevelHistory
                {
                    MemberKey = person.Key,
                    KurinKey = null,
                    PlastLevel = level,
                    DateAchieved = when
                });
            }

            person.Level = ladder[^1].Level;
            person.Member.LatestPlastLevel = person.Level;
        }
    }

    private void SeedProfileVerification(IEnumerable<Person> youth, Person zvyazkovyi)
    {
        foreach (var person in youth)
        {
            var roll = _random.NextDouble();
            if (roll < 0.6)
            {
                person.Member.ProfileVerificationStatus = MemberProfileVerificationStatus.VerifiedCurrent;
            }
            else if (roll < 0.75)
            {
                person.Member.ProfileVerificationStatus = MemberProfileVerificationStatus.VerifiedStale;
            }
            else
            {
                continue;
            }

            person.Member.ProfileVerifiedAtUtc = DaysAgo(_random.Next(10, 300));
            person.Member.ProfileVerifiedByUserKey = zvyazkovyi.UserKey;
        }
    }

    // ── Проби і вмілості ─────────────────────────────────────────────────────────────────────────

    private void SeedProgress(IReadOnlyList<Person> youth, IReadOnlyDictionary<Guid, Person> mentors, Person zvyazkovyi, Guid kurinKey)
    {
        var probes = _catalog.GetProbes();
        var badgeIds = _catalog.GetBadgeIds();

        // Five people whose вмілості sit in the review queue right now, one already refused.
        var awaitingReview = youth.Where((_, i) => i % 5 == 2).Take(5).Select(p => p.Key).ToHashSet();
        var refused = youth.Where(p => !awaitingReview.Contains(p.Key)).Skip(3).First().Key;

        foreach (var person in youth)
        {
            var mentor = mentors[person.GroupKey!.Value];

            if (probes.Count > 0)
            {
                SeedProbesFor(person, probes, mentor, zvyazkovyi, kurinKey);
            }

            if (badgeIds.Count > 0)
            {
                SeedBadgesFor(person, badgeIds, mentor, kurinKey, awaitingReview.Contains(person.Key), person.Key == refused);
            }
        }
    }

    private void SeedProbesFor(Person person, IReadOnlyList<CatalogProbe> probes, Person mentor, Person zvyazkovyi, Guid kurinKey)
    {
        switch (person.Level)
        {
            case PlastLevel.Entry or PlastLevel.Prykhylnyk:
                if (_random.NextDouble() < 0.4)
                {
                    return;
                }

                SignPoints(person, probes[0], Fraction(0.05, 0.35), mentor, kurinKey, ProbeProgressStatus.InProgress, null);
                break;

            case PlastLevel.Uchasnyk:
                SignPoints(person, probes[0], Fraction(0.3, 0.8), mentor, kurinKey, ProbeProgressStatus.InProgress, null);
                break;

            case PlastLevel.Rozviduvach:
                SignPoints(person, probes[0], 1.0, mentor, kurinKey, ProbeProgressStatus.Verified, zvyazkovyi);
                if (probes.Count > 1)
                {
                    SignPoints(person, probes[1], Fraction(0.15, 0.6), mentor, kurinKey, ProbeProgressStatus.InProgress, null);
                }

                break;

            case PlastLevel.Skob:
                SignPoints(person, probes[0], 1.0, mentor, kurinKey, ProbeProgressStatus.Verified, zvyazkovyi);
                if (probes.Count > 1)
                {
                    var secondStatus = _random.NextDouble() < 0.5 ? ProbeProgressStatus.Completed : ProbeProgressStatus.Verified;
                    SignPoints(person, probes[1], 1.0, mentor, kurinKey, secondStatus, zvyazkovyi);
                }

                if (probes.Count > 2)
                {
                    SignPoints(person, probes[2], Fraction(0.05, 0.25), mentor, kurinKey, ProbeProgressStatus.InProgress, null);
                }

                break;
        }
    }

    /// <summary>
    /// Signs the first <paramref name="fraction"/> of a probe's points the way the виховник would
    /// have over the months since the person joined, then moves the probe to
    /// <paramref name="status"/> with the same trail the status handler leaves.
    /// </summary>
    private void SignPoints(
        Person person,
        CatalogProbe probe,
        double fraction,
        Person mentor,
        Guid kurinKey,
        ProbeProgressStatus status,
        Person? verifier)
    {
        var total = probe.PointIds.Count;
        if (total == 0)
        {
            return;
        }

        var signedCount = status is ProbeProgressStatus.Completed or ProbeProgressStatus.Verified
            ? total
            : Math.Clamp((int)Math.Round(total * fraction), 1, Math.Max(1, total));

        var mentorRole = SystemRole.ForOffice(LeadershipType.KV, LeadershipRole.Vykhovnyk);
        var firstSignature = Later(person.JoinedAtUtc.AddDays(30), DaysAgo(540));
        var lastSignature = DaysAgo(_random.Next(3, 40));
        if (lastSignature <= firstSignature)
        {
            lastSignature = firstSignature.AddDays(1);
        }

        var span = (lastSignature - firstSignature).TotalDays;
        for (var i = 0; i < signedCount; i++)
        {
            var signedAt = firstSignature.AddDays(span * i / Math.Max(1, signedCount - 1)).AddMinutes(_random.Next(0, 600));
            _dbContext.ProbePointProgresses.Add(new ProbePointProgress
            {
                MemberKey = person.Key,
                KurinKey = kurinKey,
                ProbeId = probe.Id,
                PointId = probe.PointIds[i],
                IsSigned = true,
                SignedAtUtc = signedAt,
                SignedByUserKey = mentor.UserKey,
                SignedByName = mentor.FullName,
                SignedByRole = mentorRole
            });
        }

        var progress = new ProbeProgress
        {
            MemberKey = person.Key,
            KurinKey = kurinKey,
            ProbeId = probe.Id,
            Status = status
        };

        if (status is ProbeProgressStatus.Completed or ProbeProgressStatus.Verified)
        {
            var completedAt = lastSignature.AddDays(1);
            progress.CompletedAtUtc = completedAt;
            progress.CompletedByUserKey = mentor.UserKey;
            progress.CompletedByName = mentor.FullName;
            progress.CompletedByRole = mentorRole;
            progress.AuditEvents.Add(new ProbeProgressAuditEvent
            {
                FromStatus = ProbeProgressStatus.InProgress,
                ToStatus = ProbeProgressStatus.Completed,
                Action = nameof(ProbeProgressStatus.Completed),
                ActorUserKey = mentor.UserKey,
                ActorName = mentor.FullName,
                ActorRole = mentorRole,
                OccurredAtUtc = completedAt,
                Note = "Усі точки підписані"
            });
        }

        if (status == ProbeProgressStatus.Verified && verifier is not null)
        {
            var verifiedAt = Earlier(progress.CompletedAtUtc!.Value.AddDays(_random.Next(2, 14)), _now.AddHours(-1));
            var verifierRole = SystemRole.ForOffice(LeadershipType.KV, LeadershipRole.Zvyazkovyi);
            progress.VerifiedAtUtc = verifiedAt;
            progress.VerifiedByUserKey = verifier.UserKey;
            progress.VerifiedByName = verifier.FullName;
            progress.VerifiedByRole = verifierRole;
            progress.AuditEvents.Add(new ProbeProgressAuditEvent
            {
                FromStatus = ProbeProgressStatus.Completed,
                ToStatus = ProbeProgressStatus.Verified,
                Action = nameof(ProbeProgressStatus.Verified),
                ActorUserKey = verifier.UserKey,
                ActorName = verifier.FullName,
                ActorRole = verifierRole,
                OccurredAtUtc = verifiedAt,
                Note = "Перевірено на сходинах куреня"
            });
        }

        _dbContext.ProbeProgresses.Add(progress);
    }

    private void SeedBadgesFor(
        Person person,
        IReadOnlyList<string> badgeIds,
        Person mentor,
        Guid kurinKey,
        bool awaitingReview,
        bool refused)
    {
        var confirmedCount = person.Level switch
        {
            PlastLevel.Skob => _random.Next(3, 6),
            PlastLevel.Rozviduvach => _random.Next(2, 4),
            PlastLevel.Uchasnyk => _random.Next(0, 3),
            _ => _random.Next(0, 2)
        };

        var taken = new HashSet<string>();
        var mentorRole = SystemRole.ForOffice(LeadershipType.KV, LeadershipRole.Vykhovnyk);

        for (var i = 0; i < confirmedCount; i++)
        {
            var badgeId = PickDistinct(badgeIds, taken);
            if (badgeId is null)
            {
                break;
            }

            var submittedAt = Later(person.JoinedAtUtc.AddDays(60), DaysAgo(_random.Next(30, 400)));
            var reviewedAt = submittedAt.AddDays(_random.Next(1, 10));
            var note = Pick(BadgeConfirmNotes);
            var progress = new BadgeProgress
            {
                MemberKey = person.Key,
                KurinKey = kurinKey,
                BadgeId = badgeId,
                Status = BadgeProgressStatus.Confirmed,
                SubmittedAtUtc = submittedAt,
                ReviewedAtUtc = reviewedAt,
                ReviewedByUserKey = mentor.UserKey,
                ReviewedByName = mentor.FullName,
                ReviewedByRole = mentorRole,
                ReviewNote = note
            };
            AddSubmittedEvent(progress, person, submittedAt);
            progress.AuditEvents.Add(new BadgeProgressAuditEvent
            {
                FromStatus = BadgeProgressStatus.Submitted,
                ToStatus = BadgeProgressStatus.Confirmed,
                Action = "Confirmed",
                ActorUserKey = mentor.UserKey,
                ActorName = mentor.FullName,
                ActorRole = mentorRole,
                OccurredAtUtc = reviewedAt,
                Note = note
            });
            _dbContext.BadgeProgresses.Add(progress);
        }

        if (awaitingReview)
        {
            var badgeId = PickDistinct(badgeIds, taken);
            if (badgeId is not null)
            {
                var submittedAt = DaysAgo(_random.Next(1, 9)).AddHours(-_random.Next(0, 20));
                var progress = new BadgeProgress
                {
                    MemberKey = person.Key,
                    KurinKey = kurinKey,
                    BadgeId = badgeId,
                    Status = BadgeProgressStatus.Submitted,
                    SubmittedAtUtc = submittedAt,
                    ReviewNote = "Здав на останніх сходинах, прошу підтвердити"
                };
                AddSubmittedEvent(progress, person, submittedAt, progress.ReviewNote);
                _dbContext.BadgeProgresses.Add(progress);
            }
        }

        if (refused)
        {
            var badgeId = PickDistinct(badgeIds, taken);
            if (badgeId is not null)
            {
                var submittedAt = DaysAgo(21);
                var reviewedAt = DaysAgo(18);
                const string note = "Бракує практичної частини — спробуй ще раз після табору";
                var progress = new BadgeProgress
                {
                    MemberKey = person.Key,
                    KurinKey = kurinKey,
                    BadgeId = badgeId,
                    Status = BadgeProgressStatus.Rejected,
                    SubmittedAtUtc = submittedAt,
                    ReviewedAtUtc = reviewedAt,
                    ReviewedByUserKey = mentor.UserKey,
                    ReviewedByName = mentor.FullName,
                    ReviewedByRole = mentorRole,
                    ReviewNote = note
                };
                AddSubmittedEvent(progress, person, submittedAt);
                progress.AuditEvents.Add(new BadgeProgressAuditEvent
                {
                    FromStatus = BadgeProgressStatus.Submitted,
                    ToStatus = BadgeProgressStatus.Rejected,
                    Action = "Rejected",
                    ActorUserKey = mentor.UserKey,
                    ActorName = mentor.FullName,
                    ActorRole = mentorRole,
                    OccurredAtUtc = reviewedAt,
                    Note = note
                });
                _dbContext.BadgeProgresses.Add(progress);
            }
        }
    }

    private static void AddSubmittedEvent(BadgeProgress progress, Person person, DateTime submittedAt, string? note = null)
    {
        progress.AuditEvents.Add(new BadgeProgressAuditEvent
        {
            FromStatus = null,
            ToStatus = BadgeProgressStatus.Submitted,
            Action = "Submitted",
            ActorUserKey = person.UserKey,
            ActorName = person.FullName,
            ActorRole = SystemRole.Member,
            OccurredAtUtc = submittedAt,
            Note = note
        });
    }

    // ── Відзначення і перестороги ─────────────────────────────────────────────────────────────────

    private void SeedAwards(IReadOnlyList<Person> youth, IReadOnlyDictionary<Guid, Person> mentors, Person zvyazkovyi, Guid kurinKey)
    {
        var senior = youth.Where(p => p.Level is PlastLevel.Skob or PlastLevel.Rozviduvach).ToList();
        if (senior.Count == 0)
        {
            return;
        }

        var lastSummer = new DateTime(_now.Month >= 8 ? _now.Year : _now.Year - 1, 7, 20, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < Math.Min(3, senior.Count); i++)
        {
            _dbContext.MemberAwards.Add(new MemberAward
            {
                MemberKey = senior[i].Key,
                KurinKey = kurinKey,
                Level = MemberAwardLevel.First,
                DateAcquired = lastSummer,
                Note = "За участь у таборі «Соколине гніздо»",
                Status = BadgeProgressStatus.Confirmed,
                SubmittedAtUtc = lastSummer.AddDays(3),
                SubmittedByUserKey = mentors[senior[i].GroupKey!.Value].UserKey,
                ReviewedAtUtc = lastSummer.AddDays(5),
                ReviewedByUserKey = zvyazkovyi.UserKey
            });
        }

        if (senior.Count > 3)
        {
            _dbContext.MemberAwards.Add(new MemberAward
            {
                MemberKey = senior[3].Key,
                KurinKey = kurinKey,
                Level = MemberAwardLevel.Second,
                DateAcquired = DaysAgo(200),
                Note = "За проведення вишколу для молодших гуртків",
                Status = BadgeProgressStatus.Confirmed,
                SubmittedAtUtc = DaysAgo(198),
                SubmittedByUserKey = zvyazkovyi.UserKey,
                ReviewedAtUtc = DaysAgo(197),
                ReviewedByUserKey = zvyazkovyi.UserKey
            });
        }

        // One still on the Зв'язковий's desk.
        var pending = senior[^1];
        _dbContext.MemberAwards.Add(new MemberAward
        {
            MemberKey = pending.Key,
            KurinKey = kurinKey,
            Level = MemberAwardLevel.First,
            DateAcquired = DaysAgo(6),
            Note = "За організацію прибирання домівки",
            Status = BadgeProgressStatus.Submitted,
            SubmittedAtUtc = DaysAgo(4),
            SubmittedByUserKey = mentors[pending.GroupKey!.Value].UserKey
        });
    }

    private void SeedWarnings(IReadOnlyList<Person> youth, IReadOnlyDictionary<Guid, Person> mentors, Person zvyazkovyi, Guid kurinKey)
    {
        if (youth.Count < 6)
        {
            return;
        }

        var active1 = youth[1];
        _dbContext.MemberWarnings.Add(new MemberWarning
        {
            MemberKey = active1.Key,
            KurinKey = kurinKey,
            Level = MemberWarningLevel.Level1,
            IssuedAtUtc = DaysAgo(20),
            ExpiresAtUtc = DaysAgo(20).AddMonths(3),
            IssuedByUserKey = mentors[active1.GroupKey!.Value].UserKey
        });

        var active2 = youth[4];
        _dbContext.MemberWarnings.Add(new MemberWarning
        {
            MemberKey = active2.Key,
            KurinKey = kurinKey,
            Level = MemberWarningLevel.Level2,
            IssuedAtUtc = DaysAgo(33),
            ExpiresAtUtc = DaysAgo(33).AddMonths(6),
            IssuedByUserKey = zvyazkovyi.UserKey
        });

        var revoked = youth[5];
        _dbContext.MemberWarnings.Add(new MemberWarning
        {
            MemberKey = revoked.Key,
            KurinKey = kurinKey,
            Level = MemberWarningLevel.Level1,
            IssuedAtUtc = DaysAgo(150),
            ExpiresAtUtc = DaysAgo(150).AddMonths(3),
            IssuedByUserKey = mentors[revoked.GroupKey!.Value].UserKey,
            RevokedAtUtc = DaysAgo(70),
            RevokedByUserKey = zvyazkovyi.UserKey
        });
    }

    // ── Календар і задачі ────────────────────────────────────────────────────────────────────────

    private void SeedAgenda(
        Guid kurinKey,
        IReadOnlyDictionary<string, Group> groups,
        IReadOnlyDictionary<string, Leadership> leaderships,
        IReadOnlyList<Person> youth,
        IReadOnlyDictionary<Guid, Person> mentors,
        Person zvyazkovyi,
        IReadOnlyDictionary<LeadershipRole, Person> kurinOffices)
    {
        // The seven closed colours the category manager offers (BRANDBOOK §2).
        var skhodyny = Category(kurinKey, "Сходини", "#2F855A", "pi pi-users", rsvp: false, duration: 120, reminder: 60);
        var tabory = Category(kurinKey, "Табори", "#B7791F", "pi pi-sun", rsvp: true, duration: null, reminder: 24 * 60,
            capacity: 30, waitlist: true, template: "Що взяти: спальник, каремат, посуд, документи, гроші на дорогу.");
        var zakhody = Category(kurinKey, "Заходи", "#2B6CB0", "pi pi-flag", rsvp: true, duration: 180, reminder: 120);
        var vyshkoly = Category(kurinKey, "Вишколи", "#0E7490", "pi pi-compass", rsvp: true, duration: 240, reminder: 24 * 60);
        var sviata = Category(kurinKey, "Свята", "#6B46C1", "pi pi-star", rsvp: false, duration: 150, reminder: 60);
        var pratsia = Category(kurinKey, "Праця", "#4A5568", "pi pi-briefcase", rsvp: false, duration: 180, reminder: null);

        var kurinnyi = kurinOffices.GetValueOrDefault(LeadershipRole.Kurinnuy) ?? youth[0];
        var pysar = kurinOffices.GetValueOrDefault(LeadershipRole.Pysar) ?? youth[1];
        var hronikar = kurinOffices.GetValueOrDefault(LeadershipRole.Hronikar) ?? youth[2];
        var skarbnyk = kurinOffices.GetValueOrDefault(LeadershipRole.Skarbnyk) ?? youth[3];

        var sokoly = groups["Соколи"];
        var levy = groups["Леви"];
        var vedmedi = groups["Ведмеді"];

        var year = _now.Year;

        // Weekly сходини: the kurin on Saturdays, each гурток on its own weekday.
        Weekly(kurinKey, "Сходини куреня", "Збір усіх гуртків у домівці. Однострій.", skhodyny, zvyazkovyi,
            DayOfWeek.Saturday, 10, 120, weeksBack: 12, Target(AgendaTargetType.Kurin, kurinKey));
        Weekly(kurinKey, "Сходини гуртка «Соколи»", null, skhodyny, mentors[sokoly.GroupKey],
            DayOfWeek.Wednesday, 17, 90, weeksBack: 10, Target(AgendaTargetType.Group, sokoly.GroupKey));
        Weekly(kurinKey, "Сходини гуртка «Леви»", null, skhodyny, mentors[levy.GroupKey],
            DayOfWeek.Friday, 16, 90, weeksBack: 10, Target(AgendaTargetType.Group, levy.GroupKey));

        // Last summer's camp, already happened; everybody answered back then.
        var summerCamp = AllDay(kurinKey, "Літній табір «Соколине гніздо»",
            "Десять днів в Арденнах біля Вільца. Виїзд о 7:00 від домівки.",
            tabory, zvyazkovyi, new DateTime(year, 7, 10), new DateTime(year, 7, 20),
            Target(AgendaTargetType.Kurin, kurinKey));
        Respond(summerCamp, youth, going: 0.85, maybe: 0.0, notGoing: 0.1, daysAgoMin: 80, daysAgoMax: 120);

        // The winter camp is ahead; RSVPs are still coming in.
        var winterCamp = AllDay(kurinKey, "Зимовий табір «Сніговий шлях»",
            "Вогези, Жерармер. Лижі й снігоступи — свої або орендовані на місці.",
            tabory, zvyazkovyi, new DateTime(year + 1, 1, 3), new DateTime(year + 1, 1, 9),
            Target(AgendaTargetType.Kurin, kurinKey));
        Respond(winterCamp, youth, going: 0.6, maybe: 0.15, notGoing: 0.1, daysAgoMin: 1, daysAgoMax: 12);

        var hike = AllDay(kurinKey, "Мандрівка Мюллерталем",
            "Два дні стежкою Мюллерталь, ночівля в кемпінгу в Бердорфі. Старші гуртки.",
            zakhody, mentors[sokoly.GroupKey], NextDate(10, 10), NextDate(10, 11),
            Target(AgendaTargetType.Group, sokoly.GroupKey), Target(AgendaTargetType.Group, vedmedi.GroupKey));
        Respond(hike, youth.Where(p => p.GroupKey == sokoly.GroupKey || p.GroupKey == vedmedi.GroupKey).ToList(),
            going: 0.7, maybe: 0.1, notGoing: 0.1, daysAgoMin: 0, daysAgoMax: 6);

        Timed(kurinKey, "Пластова ватра до Дня Незалежності", "У парку Мерль, з батьками.",
            zakhody, zvyazkovyi, new DateTime(year, 8, 24), 19, 150, Target(AgendaTargetType.Kurin, kurinKey));
        Timed(kurinKey, "Свято Миколая", "Вистава молодших гуртків для батьків, після — спільний стіл.",
            sviata, zvyazkovyi, new DateTime(year, 12, 19), 17, 150, Target(AgendaTargetType.Kurin, kurinKey));
        Timed(kurinKey, "Вишкіл гурткових", "Планування року, ведення гурткової хроніки, робота з молодшими.",
            vyshkoly, zvyazkovyi, _now.Date.AddDays(14), 11, 300,
            Target(AgendaTargetType.Leadership, leaderships["Kurin"].LeadershipKey),
            Target(AgendaTargetType.Leadership, leaderships["Соколи"].LeadershipKey),
            Target(AgendaTargetType.Leadership, leaderships["Леви"].LeadershipKey));
        Timed(kurinKey, "Збірка КВ", "Підсумки вересня, табір, вкладка.",
            zakhody, zvyazkovyi, NextWeekday(DayOfWeek.Tuesday), 19, 90,
            Target(AgendaTargetType.Leadership, leaderships["KV"].LeadershipKey));
        Timed(kurinKey, "Прибирання домівки", "Після ремонту в підвалі. Рукавиці свої.",
            pratsia, mentors[levy.GroupKey], _now.Date.AddDays(-6), 15, 180, Target(AgendaTargetType.Group, levy.GroupKey));

        // The task board: something in every column, with the kind of owners a провід actually has.
        BoardTask(kurinKey, "Підготувати програму зимового табору", "Три дні — виховна частина, чотири — лижі. Погодити з КВ до кінця місяця.",
            AgendaItemStatus.InProgress, zvyazkovyi, due: _now.Date.AddDays(30), Target(AgendaTargetType.Leadership, leaderships["Kurin"].LeadershipKey));
        BoardTask(kurinKey, "Зібрати вкладку за вересень", "Гурткові скарбники здають курінному скарбнику на сходинах.",
            AgendaItemStatus.Todo, kurinnyi, due: _now.Date.AddDays(9),
            Target(AgendaTargetType.Group, sokoly.GroupKey), Target(AgendaTargetType.Group, levy.GroupKey), Target(AgendaTargetType.Group, vedmedi.GroupKey));
        BoardTask(kurinKey, "Подати список на зимовий табір у станицю", "Разом із медичними довідками.",
            AgendaItemStatus.Todo, zvyazkovyi, due: _now.Date.AddDays(12), Target(AgendaTargetType.Member, zvyazkovyi.Key));
        BoardTask(kurinKey, "Написати хроніку літнього табору", "Фото — від Остапа, текст — по одному дню на сторінку.",
            AgendaItemStatus.InProgress, kurinnyi, due: _now.Date.AddDays(5), Target(AgendaTargetType.Member, hronikar.Key));
        BoardTask(kurinKey, "Оновити склад куреня в реєстрі", "Двоє нових у Левах, один вибув.",
            AgendaItemStatus.Done, zvyazkovyi, due: _now.Date.AddDays(-3), Target(AgendaTargetType.Member, pysar.Key));
        BoardTask(kurinKey, "Замовити дрова для ватри", null,
            AgendaItemStatus.Done, kurinnyi, due: _now.Date.AddDays(-30), Target(AgendaTargetType.Member, skarbnyk.Key));
        BoardTask(kurinKey, "Перевірити табірне спорядження", "Намети, казани, сокири. Список — у господаря.",
            AgendaItemStatus.Todo, kurinnyi, due: null, Target(AgendaTargetType.Group, vedmedi.GroupKey));
        BoardTask(kurinKey, "Купити прапорці для теренової гри", null,
            AgendaItemStatus.Done, mentors[sokoly.GroupKey], due: _now.Date.AddDays(-12), Target(AgendaTargetType.Group, sokoly.GroupKey));
    }

    private AgendaCategory Category(
        Guid kurinKey,
        string name,
        string color,
        string icon,
        bool rsvp,
        int? duration,
        int? reminder,
        int? capacity = null,
        bool waitlist = false,
        string? template = null)
    {
        var category = new AgendaCategory
        {
            KurinKey = kurinKey,
            Name = name,
            ColorHex = color,
            Icon = icon,
            RsvpRequired = rsvp,
            DefaultDurationMinutes = duration,
            ReminderLeadMinutes = reminder,
            Capacity = capacity,
            WaitlistEnabled = waitlist,
            DefaultDescription = template
        };
        _dbContext.AgendaCategories.Add(category);
        return category;
    }

    private static AgendaAssignment Target(AgendaTargetType type, Guid key)
        => new() { TargetType = type, TargetKey = key };

    private void Weekly(
        Guid kurinKey,
        string title,
        string? description,
        AgendaCategory category,
        Person author,
        DayOfWeek day,
        int hour,
        int minutes,
        int weeksBack,
        params AgendaAssignment[] targets)
    {
        var first = PreviousOrSame(_now.Date, day).AddDays(-7 * weeksBack);
        var start = ToUtc(first, hour);
        _dbContext.AgendaItems.Add(new AgendaItem
        {
            KurinKey = kurinKey,
            Kind = AgendaItemKind.Event,
            Title = title,
            Description = description,
            StartUtc = start,
            EndUtc = start.AddMinutes(minutes),
            IsAllDay = false,
            AgendaCategoryKey = category.AgendaCategoryKey,
            RecurrenceFrequency = RecurrenceFrequency.Weekly,
            RecurrenceInterval = 1,
            RecurrenceByWeekday = 1 << (int)day,
            CreatedByUserKey = author.UserKey,
            Assignments = targets.ToList()
        });
    }

    private AgendaItem AllDay(
        Guid kurinKey,
        string title,
        string? description,
        AgendaCategory category,
        Person author,
        DateTime firstDay,
        DateTime lastDay,
        params AgendaAssignment[] targets)
    {
        var item = new AgendaItem
        {
            KurinKey = kurinKey,
            Kind = AgendaItemKind.Event,
            Title = title,
            Description = description,
            StartUtc = DateTime.SpecifyKind(firstDay.Date, DateTimeKind.Utc),
            EndUtc = DateTime.SpecifyKind(lastDay.Date, DateTimeKind.Utc).AddHours(21),
            IsAllDay = true,
            AgendaCategoryKey = category.AgendaCategoryKey,
            CreatedByUserKey = author.UserKey,
            Assignments = targets.ToList()
        };
        _dbContext.AgendaItems.Add(item);
        return item;
    }

    private void Timed(
        Guid kurinKey,
        string title,
        string? description,
        AgendaCategory category,
        Person author,
        DateTime day,
        int hour,
        int minutes,
        params AgendaAssignment[] targets)
    {
        var start = ToUtc(day.Date, hour);
        _dbContext.AgendaItems.Add(new AgendaItem
        {
            KurinKey = kurinKey,
            Kind = AgendaItemKind.Event,
            Title = title,
            Description = description,
            StartUtc = start,
            EndUtc = start.AddMinutes(minutes),
            IsAllDay = false,
            AgendaCategoryKey = category.AgendaCategoryKey,
            CreatedByUserKey = author.UserKey,
            Assignments = targets.ToList()
        });
    }

    private void BoardTask(
        Guid kurinKey,
        string title,
        string? description,
        AgendaItemStatus status,
        Person author,
        DateTime? due,
        params AgendaAssignment[] targets)
    {
        var start = due is null ? (DateTime?)null : DateTime.SpecifyKind(due.Value.Date, DateTimeKind.Utc);
        _dbContext.AgendaItems.Add(new AgendaItem
        {
            KurinKey = kurinKey,
            Kind = AgendaItemKind.Task,
            Title = title,
            Description = description,
            Status = status,
            StartUtc = start,
            EndUtc = null,
            IsAllDay = true,
            CreatedByUserKey = author.UserKey,
            Assignments = targets.ToList()
        });
    }

    private void Respond(AgendaItem item, IReadOnlyList<Person> people, double going, double maybe, double notGoing, int daysAgoMin, int daysAgoMax)
    {
        foreach (var person in people)
        {
            var roll = _random.NextDouble();
            AgendaRsvpStatus status;
            if (roll < going)
            {
                status = AgendaRsvpStatus.Going;
            }
            else if (roll < going + maybe)
            {
                status = AgendaRsvpStatus.Maybe;
            }
            else if (roll < going + maybe + notGoing)
            {
                status = AgendaRsvpStatus.NotGoing;
            }
            else
            {
                continue;
            }

            item.Responses.Add(new AgendaResponse
            {
                UserKey = person.UserKey,
                Status = status,
                RespondedAtUtc = DaysAgo(_random.Next(daysAgoMin, daysAgoMax + 1)).AddMinutes(-_random.Next(0, 900))
            });
        }
    }

    // ── Планування ───────────────────────────────────────────────────────────────────────────────

    private void SeedPlanning(Guid kurinKey, IReadOnlyList<Person> youth, IReadOnlyDictionary<Guid, Person> mentors, Person zvyazkovyi)
    {
        var year = _now.Year;

        var winter = new PlanningSession
        {
            KurinKey = kurinKey,
            Name = $"Зимовий табір {year + 1}",
            CreatedByUserKey = zvyazkovyi.UserKey,
            SearchStart = new DateTime(year, 12, 26, 0, 0, 0, DateTimeKind.Utc),
            SearchEnd = new DateTime(year + 1, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            DurationDays = 7,
            OptimalStartDate = new DateTime(year + 1, 1, 3, 0, 0, 0, DateTimeKind.Utc),
            OptimalEndDate = new DateTime(year + 1, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            ConflictScore = 1.5,
            IsCalculated = true
        };

        winter.Participants.Add(Participant(zvyazkovyi, 2.0, (new DateTime(year, 12, 30), new DateTime(year + 1, 1, 2))));
        foreach (var mentor in mentors.Values)
        {
            winter.Participants.Add(Participant(mentor, 1.5, (new DateTime(year + 1, 1, 12), new DateTime(year + 1, 1, 16))));
        }

        foreach (var person in youth.Take(12))
        {
            var busy = _random.NextDouble() < 0.5
                ? (new DateTime(year, 12, 31), new DateTime(year + 1, 1, 2))
                : (new DateTime(year + 1, 1, 13), new DateTime(year + 1, 1, 18));
            winter.Participants.Add(Participant(person, 1.0, busy));
        }

        _dbContext.PlanningSessions.Add(winter);

        var alps = new PlanningSession
        {
            KurinKey = kurinKey,
            Name = "Мандрівка в Альпи",
            CreatedByUserKey = zvyazkovyi.UserKey,
            SearchStart = new DateTime(year + 1, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            SearchEnd = new DateTime(year + 1, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            DurationDays = 4,
            IsCalculated = false
        };

        foreach (var person in youth.Skip(12).Take(6))
        {
            alps.Participants.Add(Participant(person, 1.0, (new DateTime(year + 1, 5, 8), new DateTime(year + 1, 5, 11))));
        }

        _dbContext.PlanningSessions.Add(alps);
    }

    private static PlanningParticipant Participant(Person person, double weight, (DateTime Start, DateTime End) busy)
    {
        var participant = new PlanningParticipant
        {
            MemberKey = person.Key,
            FullName = person.FullName,
            RoleWeight = weight
        };
        participant.BusyRanges.Add(new ParticipantBusyRange
        {
            Start = DateTime.SpecifyKind(busy.Start, DateTimeKind.Utc),
            End = DateTime.SpecifyKind(busy.End, DateTimeKind.Utc)
        });
        return participant;
    }

    // ── Провід ───────────────────────────────────────────────────────────────────────────────────

    private async Task<Leadership> EnsureLeadershipAsync(
        LeadershipType type,
        Guid? kurinKey,
        Guid? groupKey,
        DateOnly startDate,
        CancellationToken cancellationToken)
    {
        var leadership = await _dbContext.Leaderships
            .Include(l => l.LeadershipHistories)
            .FirstOrDefaultAsync(l => l.Type == type
                && (kurinKey == null || l.KurinKey == kurinKey)
                && (groupKey == null || l.GroupKey == groupKey), cancellationToken);

        if (leadership == null)
        {
            leadership = new Leadership
            {
                Type = type,
                KurinKey = kurinKey,
                GroupKey = groupKey,
                StartDate = startDate
            };
            _dbContext.Leaderships.Add(leadership);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return leadership;
    }

    private static Dictionary<LeadershipRole, Person> SeatOffices(Leadership leadership, IReadOnlyList<Person> people, LeadershipRole[] offices, DateOnly since)
    {
        var holders = new Dictionary<LeadershipRole, Person>();
        for (var i = 0; i < people.Count && i < offices.Length; i++)
        {
            SeatOffice(leadership, people[i].Key, offices[i], since);
            holders[offices[i]] = people[i];
        }

        return holders;
    }

    /// <summary>
    /// Seats a person in an office from a given day — <c>AddOffice</c> dates every row today, which
    /// no каденція does. A closed row (with <paramref name="until"/>) is history, not a current seat.
    /// </summary>
    private static void SeatOffice(Leadership leadership, Guid memberKey, LeadershipRole role, DateOnly since, DateOnly? until = null)
    {
        var existing = leadership.LeadershipHistories
            .FirstOrDefault(h => h.MemberKey == memberKey && h.Role == role && h.EndDate == until);
        if (existing is not null)
        {
            existing.StartDate = since;
            return;
        }

        leadership.LeadershipHistories.Add(new LeadershipHistory
        {
            MemberKey = memberKey,
            Role = role,
            StartDate = since,
            EndDate = until
        });
    }

    // ── Дрібниці ─────────────────────────────────────────────────────────────────────────────────

    private T Pick<T>(IReadOnlyList<T> items) => items[_random.Next(items.Count)];

    private string? PickDistinct(IReadOnlyList<string> items, HashSet<string> taken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = Pick(items);
            if (taken.Add(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private double Fraction(double min, double max) => min + _random.NextDouble() * (max - min);

    private DateTime DaysAgo(int days) => _now.AddDays(-days);

    private DateOnly YearsAgo(int years) => DateOnly.FromDateTime(_now.AddYears(-years));

    private static DateTime Later(DateTime a, DateTime b) => a > b ? a : b;

    private static DateTime Earlier(DateTime a, DateTime b) => a < b ? a : b;

    private int AgeOf(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(_now);
        var age = today.Year - dateOfBirth.Year;
        return dateOfBirth > today.AddYears(-age) ? age - 1 : age;
    }

    private DateTime NextDate(int month, int day)
    {
        var candidate = new DateTime(_now.Year, month, day);
        return candidate.Date < _now.Date ? candidate.AddYears(1) : candidate;
    }

    private DateTime NextWeekday(DayOfWeek day)
    {
        var date = _now.Date.AddDays(1);
        while (date.DayOfWeek != day)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private static DateTime PreviousOrSame(DateTime date, DayOfWeek day)
    {
        while (date.DayOfWeek != day)
        {
            date = date.AddDays(-1);
        }

        return date;
    }

    /// <summary>A wall-clock hour in Kyiv on the given day, as the UTC instant the calendar stores.</summary>
    private static DateTime ToUtc(DateTime day, int hour)
    {
        var local = DateTime.SpecifyKind(day.Date.AddHours(hour), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, Kyiv);
    }

    private static TimeZoneInfo ResolveKyiv()
    {
        foreach (var id in new[] { "Europe/Kyiv", "Europe/Kiev", "FLE Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Kyiv", TimeSpan.FromHours(2), "Kyiv", "Kyiv");
    }

    private string[] Shuffled(string[] source)
    {
        var copy = (string[])source.Clone();
        for (var i = copy.Length - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }

        return copy;
    }
}
