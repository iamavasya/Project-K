using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.DbContexts
{
    public class AppDbContext : DbContext
    {
        // Kurin module DbSet
        public DbSet<Kurin> Kurins { get; set; }

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
        }
    }
}
