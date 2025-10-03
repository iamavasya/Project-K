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
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(entity => entity.Kurin)
                        .WithMany(k => k.Members)
                        .HasForeignKey(e => e.KurinKey)
                        .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(entity => entity.User)
                      .WithOne()
                      .HasForeignKey<Member>(e => e.UserKey)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
