using Project_K.Infrastructure.Interfaces;
using Project_K.BusinessLogic.Interfaces;
using Project_K.Infrastructure.Models;


namespace Project_K.BusinessLogic.Services
{
    public class LevelService : ILevelService
    {
        private readonly ILevelRepository _levelRepository;

        public LevelService(ILevelRepository levelRepository)
        {
            _levelRepository = levelRepository;
        }

        public async Task<Level?> GetByIdAsync(int id)
        {
            return await _levelRepository.GetByIdAsync(id);
        }
    }
}