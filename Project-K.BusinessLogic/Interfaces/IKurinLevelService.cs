using Microsoft.AspNetCore.Mvc.Rendering;
using Project_K.BusinessLogic.Interfaces;
using Project_K.Infrastructure.Interfaces;
using Project_K.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_K.BusinessLogic.Interfaces
{
    public interface IKurinLevelService
    {
        Task<KurinLevel> GetByIdAsync(int id);
    }
}
