using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.DuesModule;
using ProjectK.Common.Interfaces.Modules.ScoreModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Infrastructure.DbContexts;
using ProjectK.Infrastructure.Repositories;
using ProjectK.Infrastructure.Repositories.AuthModule;
using ProjectK.Infrastructure.Repositories.DuesModule;
using ProjectK.Infrastructure.Repositories.ScoreModule;
using ProjectK.Infrastructure.Repositories.InfrastructureModule;
using ProjectK.Infrastructure.Repositories.KurinModule;
using ProjectK.Infrastructure.Repositories.ProbesAndBadgesModule;

namespace ProjectK.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork, IMemberUnitOfWork, IDuesUnitOfWork, IScoreUnitOfWork
{
    private readonly AppDbContext _context;

    // Repositories are created on first access, not up front: a typical request
    // touches one or two of them, so eagerly newing all 17 was 15-16 wasted
    // allocations per scoped UnitOfWork. Backing fields (not Lazy<T>) keep it a
    // single allocation per repo actually used — the UoW is scoped per request
    // and used single-threaded, so no synchronisation is needed.
    private IKurinRepository _kurins;
    private IGroupRepository _groups;
    private IMembershipRepository _memberships;
    private IMemberRepository _members;
    private ILeadershipRepository _leaderships;
    private IPlanningSessionRepository _planningSessions;
    private IAgendaItemRepository _agendaItems;
    private IAgendaCategoryRepository _agendaCategories;
    private IAgendaResponseRepository _agendaResponses;
    private IBadgeProgressRepository _badgeProgresses;
    private IProbeProgressRepository _probeProgresses;
    private IProbePointProgressRepository _probePointProgresses;
    private IMentorAssignmentRepository _mentorAssignments;
    private IMemberWarningRepository _memberWarnings;
    private IMemberAwardRepository _memberAwards;
    private IWaitlistRepository _waitlistEntries;
    private IInvitationRepository _invitations;
    private IAppNotificationRepository _appNotifications;
    private ISystemSettingRepository _systemSettings;
    private IAppUserRepository _users;
    private IUserTileLayoutRepository _userTileLayouts;
    private IKurinDuesRateRepository _kurinDuesRates;
    private IGroupDuesRateRepository _groupDuesRates;
    private IDuesConcessionRepository _duesConcessions;
    private IDuesChargeRepository _duesCharges;
    private IDuesEntryRepository _duesEntries;
    private IKurinScoreSettingsRepository _kurinScoreSettings;
    private IScoreRuleRepository _scoreRules;
    private IScoreAttendanceRateRepository _scoreAttendanceRates;
    private IScoreItemRepository _scoreItems;
    private IScoreStageRepository _scoreStages;
    private IScoreAttendanceRepository _scoreAttendances;
    private IScoreEntryRepository _scoreEntries;
    private IScoreGroupMoveRepository _scoreGroupMoves;
    private IScoreTrailEventRepository _scoreTrailEvents;
    private IPrivateScoreCriterionRepository _privateScoreCriteria;
    private IPrivateScoreEntryRepository _privateScoreEntries;

    public IKurinRepository Kurins => _kurins ??= new KurinRepository(_context);
    public IGroupRepository Groups => _groups ??= new GroupRepository(_context);
    public IMembershipRepository Memberships => _memberships ??= new MembershipRepository(_context);
    public IMemberRepository Members => _members ??= new MemberRepository(_context);
    public IAppUserRepository Users => _users ??= new AppUserRepository(_context);
    public ILeadershipRepository Leaderships => _leaderships ??= new LeadershipRepository(_context);
    public IPlanningSessionRepository PlanningSessions => _planningSessions ??= new PlanningSessionRepository(_context);
    public IAgendaItemRepository AgendaItems => _agendaItems ??= new AgendaItemRepository(_context);
    public IAgendaCategoryRepository AgendaCategories => _agendaCategories ??= new AgendaCategoryRepository(_context);
    public IAgendaResponseRepository AgendaResponses => _agendaResponses ??= new AgendaResponseRepository(_context);
    public IBadgeProgressRepository BadgeProgresses => _badgeProgresses ??= new BadgeProgressRepository(_context);
    public IProbeProgressRepository ProbeProgresses => _probeProgresses ??= new ProbeProgressRepository(_context);
    public IProbePointProgressRepository ProbePointProgresses => _probePointProgresses ??= new ProbePointProgressRepository(_context);
    public IMentorAssignmentRepository MentorAssignments => _mentorAssignments ??= new MentorAssignmentRepository(_context);
    public IMemberWarningRepository MemberWarnings => _memberWarnings ??= new MemberWarningRepository(_context);
    public IMemberAwardRepository MemberAwards => _memberAwards ??= new MemberAwardRepository(_context);
    public IWaitlistRepository WaitlistEntries => _waitlistEntries ??= new WaitlistRepository(_context);
    public IInvitationRepository Invitations => _invitations ??= new InvitationRepository(_context);
    public IAppNotificationRepository AppNotifications => _appNotifications ??= new AppNotificationRepository(_context);
    public ISystemSettingRepository SystemSettings => _systemSettings ??= new SystemSettingRepository(_context);
    public IUserTileLayoutRepository UserTileLayouts => _userTileLayouts ??= new UserTileLayoutRepository(_context);
    public IKurinDuesRateRepository KurinDuesRates => _kurinDuesRates ??= new KurinDuesRateRepository(_context);
    public IGroupDuesRateRepository GroupDuesRates => _groupDuesRates ??= new GroupDuesRateRepository(_context);
    public IDuesConcessionRepository DuesConcessions => _duesConcessions ??= new DuesConcessionRepository(_context);
    public IDuesChargeRepository DuesCharges => _duesCharges ??= new DuesChargeRepository(_context);
    public IDuesEntryRepository DuesEntries => _duesEntries ??= new DuesEntryRepository(_context);
    public IKurinScoreSettingsRepository KurinScoreSettings => _kurinScoreSettings ??= new KurinScoreSettingsRepository(_context);
    public IScoreRuleRepository ScoreRules => _scoreRules ??= new ScoreRuleRepository(_context);
    public IScoreAttendanceRateRepository ScoreAttendanceRates => _scoreAttendanceRates ??= new ScoreAttendanceRateRepository(_context);
    public IScoreItemRepository ScoreItems => _scoreItems ??= new ScoreItemRepository(_context);
    public IScoreStageRepository ScoreStages => _scoreStages ??= new ScoreStageRepository(_context);
    public IScoreAttendanceRepository ScoreAttendances => _scoreAttendances ??= new ScoreAttendanceRepository(_context);
    public IScoreEntryRepository ScoreEntries => _scoreEntries ??= new ScoreEntryRepository(_context);
    public IScoreGroupMoveRepository ScoreGroupMoves => _scoreGroupMoves ??= new ScoreGroupMoveRepository(_context);
    public IScoreTrailEventRepository ScoreTrailEvents => _scoreTrailEvents ??= new ScoreTrailEventRepository(_context);
    public IPrivateScoreCriterionRepository PrivateScoreCriteria => _privateScoreCriteria ??= new PrivateScoreCriterionRepository(_context);
    public IPrivateScoreEntryRepository PrivateScoreEntries => _privateScoreEntries ??= new PrivateScoreEntryRepository(_context);

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken token = default)
    {
        return _context.SaveChangesAsync(token);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken token = default)
    {
        return new EfCoreUnitOfWorkTransaction(await _context.Database.BeginTransactionAsync(token));
    }
}
