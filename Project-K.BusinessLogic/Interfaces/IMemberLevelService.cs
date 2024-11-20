using Project_K.BusinessLogic.Dtos;
using Project_K.Infrastructure.Models;

namespace Project_K.BusinessLogic.Interfaces
{
    public interface IMemberLevelService
    {
        Task AddMemberLevel(int id, int levelId, DateOnly? date = null);
        Task UpdateMemberLevel(int id, MemberLevel? memberLevel, int selectedLevelId, DateOnly? date = null);
    }
}