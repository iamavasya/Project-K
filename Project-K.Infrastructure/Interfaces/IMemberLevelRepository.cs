using Project_K.Infrastructure.Models;

namespace Project_K.Infrastructure.Interfaces
{
    public interface IMemberLevelRepository
    {
        Task AddAsync(MemberLevel memberLevel);
        Task UpdateAsync(MemberLevel memberLevel);
    }
}