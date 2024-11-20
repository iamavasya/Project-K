using Project_K.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project_K.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Project_K.Infrastructure.Data;

namespace Project_K.Infrastructure.Repositories
{
    public class TeamRepository : ITeamRepository
    {
        private readonly KurinDbContext _context;
        public TeamRepository(KurinDbContext context)
        {
            _context = context;
        }
        public DbSet<Team> GetTeams()
        {
            return _context.Teams;
        }
        public async Task<Team?> GetByIdAsync(int id)
        {
            return await _context.Teams.FindAsync(id);
        }
    }
}
