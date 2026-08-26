using ProjectK.Common.Models.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.API.Helpers
{
    /// <summary>
    /// Seeds a realistic kurin for local testing: three гуртки with діловодські offices, a курінний
    /// провід, a КВ (Зв'язковий + Впорядники), and mentor assignments — everyone a bare Member whose
    /// access is derived from their office via <see cref="ILeadershipRoleSyncService"/>.
    /// </summary>
    public class DemoDataSeeder : IDemoDataSeeder
    {

        private static readonly string[] FirstNames =
        {
            "Андрій", "Богдан", "Василь", "Григорій", "Дмитро", "Остап", "Ігор", "Тарас",
            "Юрій", "Роман", "Степан", "Микола", "Олег", "Павло", "Сергій", "Назар",
            "Максим", "Орест", "Левко", "Артем", "Данило", "Марко", "Захар", "Устим",
            "Ярослав", "Мирослав", "Святослав", "Володимир", "Любомир", "Ростислав"
        };

        private static readonly string[] LastNames =
        {
            "Шевченко", "Франко", "Коваль", "Бондаренко", "Мельник", "Ткаченко", "Кравчук", "Гнатюк",
            "Панчук", "Савчук", "Романюк", "Дідух", "Іваненко", "Кузьменко", "Лисенко", "Марчук",
            "Гаврилюк", "Оліярник", "Соловей", "Вербицький", "Гончар", "Пасічник", "Цимбалюк", "Яремчук",
            "Стельмах", "Чорновіл", "Кушнір", "Бойчук", "Сорока", "Левицький"
        };

        // Taken from the office registry rather than restated: the local copy had already lost
        // Hronikar from the гуртковий провід, so demo data no longer matched what the app allows.
        private static readonly LeadershipRole[] GroupOffices =
            LeadershipOffices.Grouping[LeadershipType.Group].ToArray();

        private static readonly LeadershipRole[] KurinOffices =
            LeadershipOffices.Grouping[LeadershipType.Kurin].ToArray();

        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILeadershipRoleSyncService _roleSync;

        private int _personIndex;
        private int _emailIndex;

        public DemoDataSeeder(AppDbContext dbContext, UserManager<AppUser> userManager, ILeadershipRoleSyncService roleSync)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleSync = roleSync;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            var membersToSync = new HashSet<Guid>();

            // 1. Kurin
            var kurin = await _dbContext.Kurins.FirstOrDefaultAsync(k => k.Number == 1, cancellationToken);
            if (kurin == null)
            {
                kurin = new Kurin(1) { IsZbtKurin = true };
                _dbContext.Kurins.Add(kurin);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // 2. Groups: two ordinary гуртки and one провідний (its members form the курінний провід).
            var sokoly = await DataSeeder.EnsureGroupAsync(_dbContext, "Соколи", kurin.KurinKey, cancellationToken);
            var levy = await DataSeeder.EnsureGroupAsync(_dbContext, "Леви", kurin.KurinKey, cancellationToken);
            var vedmedi = await DataSeeder.EnsureGroupAsync(_dbContext, "Ведмеді", kurin.KurinKey, cancellationToken);

            // 3. Зв'язковий — adult, whole-kurin authority.
            var zvyazkovyi = await CreateMemberAsync(kurin.KurinKey, null, cancellationToken);
            var kvLeadership = await EnsureLeadershipAsync(LeadershipType.KV, kurin.KurinKey, null, cancellationToken);
            DataSeeder.AddOffice(kvLeadership, zvyazkovyi.MemberKey, LeadershipRole.Zvyazkovyi);
            membersToSync.Add(zvyazkovyi.MemberKey);

            // 4. Ordinary гуртки: 8 members each, first 6 hold гуртковий-провід offices.
            foreach (var group in new[] { sokoly, levy })
            {
                var leadership = await EnsureLeadershipAsync(LeadershipType.Group, null, group.GroupKey, cancellationToken);
                for (var i = 0; i < 8; i++)
                {
                    var member = await CreateMemberAsync(kurin.KurinKey, group.GroupKey, cancellationToken);
                    if (i < GroupOffices.Length)
                    {
                        DataSeeder.AddOffice(leadership, member.MemberKey, GroupOffices[i]);
                        membersToSync.Add(member.MemberKey);
                    }
                }
            }

            // 5. Провідний гурток "Ведмеді": its 8 members form the курінний провід.
            var kurinLeadership = await EnsureLeadershipAsync(LeadershipType.Kurin, kurin.KurinKey, null, cancellationToken);
            for (var i = 0; i < 8; i++)
            {
                var member = await CreateMemberAsync(kurin.KurinKey, vedmedi.GroupKey, cancellationToken);
                DataSeeder.AddOffice(kurinLeadership, member.MemberKey, KurinOffices[i]);
                membersToSync.Add(member.MemberKey);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // 6. One Впорядник (mentor) per гурток: a КВ Впорядник office plus a mentor assignment that
            //    scopes their group access. Both the КВ table and the mentor list are filled this way.
            foreach (var group in new[] { sokoly, levy, vedmedi })
            {
                var mentor = await CreateMemberAsync(kurin.KurinKey, null, cancellationToken);
                DataSeeder.AddOffice(kvLeadership, mentor.MemberKey, LeadershipRole.Vykhovnyk);
                _dbContext.MentorAssignments.Add(new MentorAssignment
                {
                    MentorUserKey = mentor.UserKey!.Value,
                    GroupKey = group.GroupKey,
                    AssignedAtUtc = DateTime.UtcNow
                });
                membersToSync.Add(mentor.MemberKey);
            }

            // 7. One Інструктор in the КВ.
            var instructor = await CreateMemberAsync(kurin.KurinKey, null, cancellationToken);
            DataSeeder.AddOffice(kvLeadership, instructor.MemberKey, LeadershipRole.Instruktor);
            membersToSync.Add(instructor.MemberKey);

            await _dbContext.SaveChangesAsync(cancellationToken);

            // 8. Derive system roles from the offices and assignments just created.
            await _roleSync.SyncMembersAsync(membersToSync, cancellationToken);
        }

        private async Task<Member> CreateMemberAsync(Guid kurinKey, Guid? groupKey, CancellationToken cancellationToken)
        {
            var firstName = FirstNames[_personIndex % FirstNames.Length];
            var lastName = LastNames[_personIndex % LastNames.Length];
            _personIndex++;

            var email = $"demo{_emailIndex++}@projectk.com";

            return await DataSeeder.EnsureMemberAsync(
                _dbContext,
                _userManager,
                email,
                firstName,
                lastName,
                kurinKey,
                groupKey,
                $"050{_emailIndex:D7}",
                new DateOnly(2005, 1, 1).AddDays(_personIndex * 37),
                cancellationToken);
        }

        private async Task<Leadership> EnsureLeadershipAsync(
            LeadershipType type,
            Guid? kurinKey,
            Guid? groupKey,
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
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };
                _dbContext.Leaderships.Add(leadership);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return leadership;
        }

    }
}
