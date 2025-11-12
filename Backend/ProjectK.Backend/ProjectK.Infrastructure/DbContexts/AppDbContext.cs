using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
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

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Kurin module entity configuration
            modelBuilder.Entity<Kurin>(entity =>
            {
                entity.HasKey(e => e.KurinKey);
                entity.HasIndex(e => e.Number).IsUnique();
            });

            modelBuilder.Entity<Group>(entity =>
            {
                entity.HasKey(e => e.GroupKey);
                entity.HasOne(e => e.Kurin)
                      .WithMany(k => k.Groups)
                      .HasForeignKey(e => e.KurinKey)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Member>(entity =>
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

            modelBuilder.Entity<PlastLevelHistory>(entity =>
            {
                entity.HasKey(e => e.PlastLevelHistoryKey);
                entity.HasOne(e => e.Member)
                      .WithMany(m => m.PlastLevelHistory)
                      .HasForeignKey(e => e.MemberKey)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.PlastLevel)
                      .HasConversion<int>();
            });

            modelBuilder.Entity<Leadership>(entity =>
            {
                entity.HasKey(e => e.LeadershipKey);
                entity.Property(e => e.Type)
                      .HasConversion<int>();
                entity.HasIndex(e => new { e.Type, e.KurinKey, e.GroupKey});
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

            modelBuilder.Entity<LeadershipHistory>(entity =>
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
        }
    }
}
