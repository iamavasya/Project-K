using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Models
{
    public class UserDto
    {
        public Guid UserId { get; set; }
        public Guid? KurinKey { get; set; }
        public int? KurinNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}
