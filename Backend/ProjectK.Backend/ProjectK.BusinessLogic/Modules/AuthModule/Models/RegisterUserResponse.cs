using ProjectK.Common.Models.Dtos.AuthModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Models
{
    public class RegisterUserResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public JwtResponse Tokens { get; set; } = null!;
    }
}
