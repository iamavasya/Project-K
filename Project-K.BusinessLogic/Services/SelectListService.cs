using Project_K.BusinessLogic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Project_K.Infrastructure.Interfaces;

namespace Project_K.BusinessLogic.Services
{
    public class SelectListService : ISelectListService
    {
        private readonly ILevelRepository _levelsRepository;
        private readonly IKurinLevelRepository _kurinLevelRepository;
        private readonly ITeamRepository _teamRepository;
        public SelectListService(ILevelRepository levelsRepository, IKurinLevelRepository kurinLevelRepository, ITeamRepository teamRepository)
        {
            _levelsRepository = levelsRepository;
            _kurinLevelRepository = kurinLevelRepository;
            _teamRepository = teamRepository;
        }
        public SelectList GetSelectList(string listName)
        {
            switch (listName)
            {
                case "Levels":
                    return new SelectList(_levelsRepository.GetLevels(), "Id", "Name");
                case "KurinLevels":
                    return new SelectList(_kurinLevelRepository.GetKurinLevels(), "Id", "Name");
                case "Teams":
                    return new SelectList(_teamRepository.GetTeams(), "Id", "Name");
                default:
                    return null;
            }
        }
    }
}
