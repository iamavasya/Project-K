using Project_K.Infrastructure.Data;
using Project_K.Infrastructure.Interfaces;
using Project_K.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_K.Infrastructure.Repositories
{
    public class MemberRepository : IMemberRepository
    {
        private readonly KurinDbContext _context;

        public MemberRepository(KurinDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Member>> GetMembersAsync()
        {
            return await _context.Members.Include(m => m.KurinLevel)
                                         .Include(m => m.Team)
                                         .Include(m => m.MemberLevels).ToListAsync();
        }

        public async Task<List<Member?>> GetMembersDetailed()
        {
            return await _context.Members
                .Include(m => m.KurinLevel)
                .Include(m => m.Team)
                .Include(m => m.MemberLevels)
                .ThenInclude(ml => ml.Level)
                .ToListAsync();
        }

        public async Task<Member?> GetMemberDetailed(uint id){
            return await _context.Members
                .Include(m => m.KurinLevel)
                .Include(m => m.Team)
                .Include(m => m.MemberLevels)
                .ThenInclude(ml => ml.Level)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Member?> GetByIdAsync(uint id)
        {
            return await _context.Members
                .Include(m => m.KurinLevel)
                .Include(m => m.Team)
                .Include(m => m.MemberLevels)
                .ThenInclude(ml => ml.Level)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task AddAsync(Member member)
        {
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Member member)
        {
            _context.Members.Update(member);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Member member)
        {
            _context.Members.Remove(member);
            await _context.SaveChangesAsync();
        }
    }
}
