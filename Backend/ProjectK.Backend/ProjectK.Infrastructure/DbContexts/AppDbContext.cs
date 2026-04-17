using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.KurinModule.Planning;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.DbContexts
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
    {
        // Kurin module DbSet
        public DbSet<Kurin> Kurins { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<PlastLevelHistory> PlastLevelHistories { get; set; }
        public DbSet<Leadership> Leaderships { get; set; }
        public DbSet<LeadershipHistory> LeadershipHistories { get; set; }
        public DbSet<PlanningSession> PlanningSessions { get; set; }
        public DbSet<PlanningParticipant> PlanningParticipants { get; set; }
        public DbSet<ParticipantBusyRange> ParticipantBusyRanges { get; set; }
        public DbSet<BadgeProgress> BadgeProgresses { get; set; }
        public DbSet<BadgeProgressAuditEvent> BadgeProgressAuditEvents { get; set; }
        public DbSet<ProbeProgress> ProbeProgresses { get; set; }
        public DbSet<ProbeProgressAuditEvent> ProbeProgressAuditEvents { get; set; }
        public DbSet<ProbePointProgress> ProbePointProgresses { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Kurin module entity configuration
            builder.Entity<Kurin>(entity =>
            {
                entity.HasKey(e => e.KurinKey);
                entity.HasIndex(e => e.Number).IsUnique();
            });

            builder.Entity<Group>(entity =>
            {
                entity.HasKey(e => e.GroupKey);
                entity.HasOne(e => e.Kurin)
                      .WithMany(k => k.Groups)
                      .HasForeignKey(e => e.KurinKey)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Member>(entity =>
            {
                entity.HasKey(e => e.MemberKey);
                entity.HasOne(entity => entity.Group)
                      .WithMany(g => g.Members)
                      .HasForeignKey(e => e.GroupKey)
                      .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(entity => entity.Kurin)
                        .WithMany(k => k.Members)
                        .HasForeignKey(e => e.KurinKey)
                        .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(entity => entity.User)
                      .WithOne()
                      .HasForeignKey<Member>(e => e.UserKey)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<PlastLevelHistory>(entity =>
            {
                entity.HasKey(e => e.PlastLevelHistoryKey);
                entity.HasOne(e => e.Member)
                      .WithMany(m => m.PlastLevelHistory)
                      .HasForeignKey(e => e.MemberKey)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.PlastLevel)
                      .HasConversion<int>();
            });

            builder.Entity<Leadership>(entity =>
            {
                entity.HasKey(e => e.LeadershipKey);
                entity.Property(e => e.Type)
                      .HasConversion<int>();
                entity.HasIndex(e => new { e.Type, e.KurinKey, e.GroupKey });
                entity.HasOne(e => e.Kurin)
                    .WithMany(k => k.Leaderships)
                    .HasForeignKey(e => e.KurinKey)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Group)
                    .WithOne(g => g.Leadership)
                    .HasForeignKey<Leadership>(e => e.GroupKey)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<LeadershipHistory>(entity =>
            {
                entity.HasKey(e => e.LeadershipHistoryKey);
                entity.HasOne(e => e.Member)
                      .WithMany(m => m.LeadershipHistories)
                      .HasForeignKey(e => e.MemberKey)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Leadership)
                      .WithMany(l => l.LeadershipHistories)
                      .HasForeignKey(e => e.LeadershipKey)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.LeadershipKey, e.Role, e.StartDate });
                entity.HasIndex(e => new { e.MemberKey, e.StartDate });
            });

            builder.Entity<PlanningSession>(entity =>
            {
                entity.HasKey(e => e.PlanningSessionKey);
                entity.HasOne(e => e.Kurin)
                      .WithMany(k => k.PlanningSessions)
                      .HasForeignKey(e => e.KurinKey)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<PlanningParticipant>(entity =>
            {
                entity.HasKey(e => e.PlanningParticipantKey);
                entity.HasOne(e => e.PlanningSession)
                      .WithMany(ps => ps.Participants)
                      .HasForeignKey(e => e.PlanningSessionKey)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ParticipantBusyRange>(entity =>
            {
                entity.HasKey(e => e.ParticipantBusyRangeKey);
                entity.HasOne(e => e.PlanningParticipant)
                      .WithMany(pp => pp.BusyRanges)
                      .HasForeignKey(e => e.PlanningParticipantKey)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<BadgeProgress>(entity =>
            {
                entity.HasKey(e => e.BadgeProgressKey);
                entity.Property(e => e.BadgeId)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.Property(e => e.Status)
                    .HasConversion<int>();
                entity.HasIndex(e => new { e.MemberKey, e.BadgeId })
                    .IsUnique();
                entity.HasOne(e => e.Member)
                    .WithMany(m => m.BadgeProgresses)
                    .HasForeignKey(e => e.MemberKey)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<BadgeProgressAuditEvent>(entity =>
            {
                entity.HasKey(e => e.BadgeProgressAuditEventKey);
                entity.Property(e => e.FromStatus)
                    .HasConversion<int?>();
                entity.Property(e => e.ToStatus)
                    .HasConversion<int>();
                entity.Property(e => e.Action)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(e => e.ActorRole)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.HasOne(e => e.BadgeProgress)
                    .WithMany(p => p.AuditEvents)
                    .HasForeignKey(e => e.BadgeProgressKey)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.BadgeProgressKey);
            });

            builder.Entity<ProbeProgress>(entity =>
            {
                entity.HasKey(e => e.ProbeProgressKey);
                entity.Property(e => e.ProbeId)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.Property(e => e.Status)
                    .HasConversion<int>();
                entity.HasIndex(e => new { e.MemberKey, e.ProbeId })
                    .IsUnique();
                entity.HasOne(e => e.Member)
                    .WithMany(m => m.ProbeProgresses)
                    .HasForeignKey(e => e.MemberKey)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ProbeProgressAuditEvent>(entity =>
            {
                entity.HasKey(e => e.ProbeProgressAuditEventKey);
                entity.Property(e => e.FromStatus)
                    .HasConversion<int?>();
                entity.Property(e => e.ToStatus)
                    .HasConversion<int>();
                entity.Property(e => e.Action)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(e => e.ActorRole)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.HasOne(e => e.ProbeProgress)
                    .WithMany(p => p.AuditEvents)
                    .HasForeignKey(e => e.ProbeProgressKey)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.ProbeProgressKey);
            });

            builder.Entity<ProbePointProgress>(entity =>
            {
                entity.HasKey(e => e.ProbePointProgressKey);
                entity.Property(e => e.ProbeId)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.Property(e => e.PointId)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.Property(e => e.SignedByRole)
                    .HasMaxLength(50);
                entity.HasIndex(e => new { e.MemberKey, e.ProbeId, e.PointId })
                    .IsUnique();
                entity.HasOne(e => e.Member)
                    .WithMany(m => m.ProbePointProgresses)
                    .HasForeignKey(e => e.MemberKey)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
