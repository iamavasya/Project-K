using Microsoft.EntityFrameworkCore.Storage;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Infrastructure.DbContexts;
using ProjectK.Infrastructure.Repositories;
using ProjectK.Infrastructure.Repositories.InfrastructureModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        // Repositories are created on first access, not up front: a typical request
        // touches one or two of them, so eagerly newing all 17 was 15-16 wasted
        // allocations per scoped UnitOfWork. Backing fields (not Lazy<T>) keep it a
        // single allocation per repo actually used — the UoW is scoped per request
        // and used single-threaded, so no synchronisation is needed.
        private IKurinRepository _kurins;
        private IGroupRepository _groups;
        private IMemberRepository _members;
        private ILeadershipRepository _leaderships;
        private IPlanningSessionRepository _planningSessions;
        private IAgendaItemRepository _agendaItems;
        private IBadgeProgressRepository _badgeProgresses;
        private IProbeProgressRepository _probeProgresses;
        private IProbePointProgressRepository _probePointProgresses;
        private IMentorAssignmentRepository _mentorAssignments;
        private IMemberWarningRepository _memberWarnings;
        private IMemberAwardRepository _memberAwards;
        private IWaitlistRepository _waitlistEntries;
        private IInvitationRepository _invitations;
        private IPublicAnnouncementRepository _publicAnnouncements;
        private IAppNotificationRepository _appNotifications;
        private ISystemSettingRepository _systemSettings;
        private IUserTileLayoutRepository _userTileLayouts;

        public IKurinRepository Kurins => _kurins ??= new KurinRepository(_context);
        public IGroupRepository Groups => _groups ??= new GroupRepository(_context);
        public IMemberRepository Members => _members ??= new MemberRepository(_context);
        public ILeadershipRepository Leaderships => _leaderships ??= new LeadershipRepository(_context);
        public IPlanningSessionRepository PlanningSessions => _planningSessions ??= new PlanningSessionRepository(_context);
        public IAgendaItemRepository AgendaItems => _agendaItems ??= new AgendaItemRepository(_context);
        public IBadgeProgressRepository BadgeProgresses => _badgeProgresses ??= new BadgeProgressRepository(_context);
        public IProbeProgressRepository ProbeProgresses => _probeProgresses ??= new ProbeProgressRepository(_context);
        public IProbePointProgressRepository ProbePointProgresses => _probePointProgresses ??= new ProbePointProgressRepository(_context);
        public IMentorAssignmentRepository MentorAssignments => _mentorAssignments ??= new MentorAssignmentRepository(_context);
        public IMemberWarningRepository MemberWarnings => _memberWarnings ??= new MemberWarningRepository(_context);
        public IMemberAwardRepository MemberAwards => _memberAwards ??= new MemberAwardRepository(_context);
        public IWaitlistRepository WaitlistEntries => _waitlistEntries ??= new WaitlistRepository(_context);
        public IInvitationRepository Invitations => _invitations ??= new InvitationRepository(_context);
        public IPublicAnnouncementRepository PublicAnnouncements => _publicAnnouncements ??= new PublicAnnouncementRepository(_context);
        public IAppNotificationRepository AppNotifications => _appNotifications ??= new AppNotificationRepository(_context);
        public ISystemSettingRepository SystemSettings => _systemSettings ??= new SystemSettingRepository(_context);
        public IUserTileLayoutRepository UserTileLayouts => _userTileLayouts ??= new UserTileLayoutRepository(_context);

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public Task<int> SaveChangesAsync(CancellationToken token = default)
        {
            return _context.SaveChangesAsync(token);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default)
        {
            return _context.Database.BeginTransactionAsync(token);
        }

        public void DetectChanges()
        {
            _context.ChangeTracker.DetectChanges();
        }
    }
}
