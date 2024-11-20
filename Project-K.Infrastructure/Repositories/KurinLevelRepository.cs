using Project_K.Infrastructure.Data;
using Project_K.Infrastructure.Interfaces;
using Project_K.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Project_K.Infrastructure.Repositories
{
    public class KurinLevelRepository : IKurinLevelRepository
    {
        private readonly KurinDbContext _context;
        public KurinLevelRepository(KurinDbContext context)
        {
            _context = context;    
        }
        public DbSet<KurinLevel> GetKurinLevels()
        {
            return _context.KurinLevels;
        }
        public async Task<KurinLevel?> GetByIdAsync(int id)
        {
            return await _context.KurinLevels.FindAsync(id);
        }
    }
}
