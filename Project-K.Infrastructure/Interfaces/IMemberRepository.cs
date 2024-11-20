using Project_K.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_K.Infrastructure.Interfaces
{
    public interface IMemberRepository
    {
        Task<IEnumerable<Member>> GetMembersAsync();
        Task<List<Member?>> GetMembersDetailed();
        Task<Member?> GetMemberDetailed(uint id);
        Task<Member?> GetByIdAsync(uint id);
        Task AddAsync(Member member);
        Task UpdateAsync(Member member);
        Task DeleteAsync(Member member);
    }
}
