using Project_K.Infrastructure.Data;
using Project_K.Infrastructure.Interfaces;
using Project_K.Infrastructure.Models;


namespace Project_K.Infrastructure.Repositories
{
    public class MemberLevelRepository : IMemberLevelRepository
    {
        private readonly KurinDbContext _context;

        public MemberLevelRepository(KurinDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(MemberLevel memberLevel)
        {
            await _context.MemberLevels.AddAsync(memberLevel);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(MemberLevel memberLevel)
        {
            _context.MemberLevels.Update(memberLevel);
            await _context.SaveChangesAsync();
        }
    }
}