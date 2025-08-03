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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=projectK_DB;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Kurin module entity configuration
            modelBuilder.Entity<Kurin>(entity =>
            {
                entity.HasKey(e => e.KurinKey);
            });
        }
    }
}
