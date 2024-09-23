using Microsoft.EntityFrameworkCore;
using Project_K.Infrastructure.Models;

namespace Project_K.Infrastructure.Data
{
    public class KurinDbContext : DbContext
    {
        public KurinDbContext(DbContextOptions<KurinDbContext> options) : base(options)
        {
        }
        
        public DbSet<KurinLevel> KurinLevels { get; set; }
        public DbSet<Level> Levels { get; set; }
        public DbSet<Member> Members  { get; set; }
        public DbSet<MemberLevel> MemberLevels { get; set; }
    }
}
