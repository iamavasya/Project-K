using ProjectK.Common.Models.Dtos.AuthModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Models
{
    public class LoginUserResponse
    {
        public Guid UserKey { get; set; }
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public JwtResponse Tokens { get; set; } = null!;
    }
}
