using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project_K.Infrastructure.Models;

namespace Project_K.Infrastructure.Interfaces
{
    public interface ILevelRepository
    {
        Task<Level?> GetByIdAsync(int id);
        DbSet<Level> GetLevels();
    }
}
