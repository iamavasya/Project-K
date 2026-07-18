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

        public IKurinRepository Kurins { get; }
        public IGroupRepository Groups { get; }
        public IMemberRepository Members { get; }
        public ILeadershipRepository Leaderships { get; }
        public IPlanningSessionRepository PlanningSessions { get; }
        public IBadgeProgressRepository BadgeProgresses { get; }
        public IProbeProgressRepository ProbeProgresses { get; }
        public IProbePointProgressRepository ProbePointProgresses { get; }
        public IMentorAssignmentRepository MentorAssignments { get; }
        public IMemberWarningRepository MemberWarnings { get; }
        public IMemberAwardRepository MemberAwards { get; }
        public IWaitlistRepository WaitlistEntries { get; }
        public IInvitationRepository Invitations { get; }
        public IPublicAnnouncementRepository PublicAnnouncements { get; }
        public IAppNotificationRepository AppNotifications { get; }
        public ISystemSettingRepository SystemSettings { get; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;

            Kurins = new KurinRepository(_context);
            Groups = new GroupRepository(_context);
            Members = new MemberRepository(_context);
            Leaderships = new LeadershipRepository(_context);
            PlanningSessions = new PlanningSessionRepository(_context);
            BadgeProgresses = new BadgeProgressRepository(_context);
            ProbeProgresses = new ProbeProgressRepository(_context);
            ProbePointProgresses = new ProbePointProgressRepository(_context);
            MentorAssignments = new MentorAssignmentRepository(_context);
            MemberWarnings = new MemberWarningRepository(_context);
            MemberAwards = new MemberAwardRepository(_context);
            WaitlistEntries = new WaitlistRepository(_context);
            Invitations = new InvitationRepository(_context);
            PublicAnnouncements = new PublicAnnouncementRepository(_context);
            AppNotifications = new AppNotificationRepository(_context);
            SystemSettings = new SystemSettingRepository(_context);
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
