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
        public JwtResponse Tokens { get; set; } = null!;
    }
}
