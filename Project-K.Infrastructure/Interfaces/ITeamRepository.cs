using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project_K.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Project_K.Infrastructure.Interfaces
{
    public interface ITeamRepository
    {
        Task<Team?> GetByIdAsync(int id);
        DbSet<Team> GetTeams();
    }
}
