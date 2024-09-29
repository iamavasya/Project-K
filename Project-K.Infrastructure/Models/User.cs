using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_K.Infrastructure.Models
{
    public class User : IdentityUser
    {
        public bool IsMemberInfoCompleted { get; set; }
    }
}
