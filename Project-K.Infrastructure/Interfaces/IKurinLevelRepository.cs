using Microsoft.AspNetCore.Mvc.Rendering;
using Project_K.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_K.Infrastructure.Interfaces
{
    public interface IKurinLevelRepository
    {
        Task<KurinLevel?> GetByIdAsync(int id);
        DbSet<KurinLevel> GetKurinLevels();
    }
}
