using Project_K.Infrastructure.Interfaces;
using Project_K.BusinessLogic.Interfaces;
using Project_K.Infrastructure.Models;


namespace Project_K.BusinessLogic.Services
{
    public class KurinLevelService : IKurinLevelService
    {
        private readonly IKurinLevelRepository _kurinLevelRepository;

        public KurinLevelService(IKurinLevelRepository kurinLevelRepository)
        {
            _kurinLevelRepository = kurinLevelRepository;
        }

        public async Task<KurinLevel?> GetByIdAsync(int id)
        {
            return await _kurinLevelRepository.GetByIdAsync(id);
        }
    }
}