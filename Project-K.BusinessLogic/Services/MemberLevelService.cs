using Project_K.BusinessLogic.Interfaces;
using Project_K.Infrastructure.Interfaces;
using Project_K.Infrastructure.Models;

namespace Project_K.BusinessLogic.Services
{
    public class MemberLevelService : IMemberLevelService
    {
        private readonly IMemberLevelRepository _memberLevelRepository;

        public MemberLevelService(IMemberLevelRepository memberLevelRepository)
        {
            _memberLevelRepository = memberLevelRepository;
        }

        public async Task AddMemberLevel(int id, int levelId, DateOnly? date = null)
        {
            var memberLevel = new MemberLevel
            {
                MemberId = id,
                LevelId = levelId,
                AchieveDate = date
            };

            await _memberLevelRepository.AddAsync(memberLevel);
        }

        public async Task UpdateMemberLevel(int id, MemberLevel? memberLevel, int selectedLevelId, DateOnly? date = null)
        {
            if (memberLevel == null)
            {
                await AddMemberLevel(id, selectedLevelId, date);
            }
            else
            {
                memberLevel.LevelId = selectedLevelId;
                memberLevel.AchieveDate = date;
                
                await _memberLevelRepository.UpdateAsync(memberLevel);
            }
        }
    }
}