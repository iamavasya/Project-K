using Microsoft.AspNetCore.Mvc.Rendering;
using Project_K.BusinessLogic.Interfaces;
using Project_K.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project_K.Infrastructure.Models;

namespace Project_K.BusinessLogic.Interfaces
{
    public interface ITeamService
    {
        Task<Team> GetByIdAsync(int id);
    }
}
