using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Project_K.Infrastructure.Data;
using Project_K.Infrastructure.Interfaces;
using Project_K.Infrastructure.Models;

namespace Project_K.Infrastructure.Repositories
{
    public class LevelRepository : ILevelRepository
    {
        private readonly KurinDbContext _context;

        public LevelRepository(KurinDbContext context)
        {
            _context = context;
        }

        public DbSet<Level> GetLevels()
        {
            return _context.Levels;
        }

        public async Task<Level?> GetByIdAsync(int id)
        {
            return await _context.Levels.FindAsync(id);
        }
    }
}
