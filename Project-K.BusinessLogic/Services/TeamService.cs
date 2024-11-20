using Project_K.Infrastructure.Interfaces;
using Project_K.BusinessLogic.Interfaces; 
using Project_K.Infrastructure.Models;


namespace Project_K.BusinessLogic.Services
{
    public class TeamService : ITeamService
    {
        private readonly ITeamRepository _teamRepository;

        public TeamService(ITeamRepository teamRepository)
        {
            _teamRepository = teamRepository;
        }

        public async Task<Team?> GetByIdAsync(int id)
        {
            return await _teamRepository.GetByIdAsync(id);
        }
    }
}