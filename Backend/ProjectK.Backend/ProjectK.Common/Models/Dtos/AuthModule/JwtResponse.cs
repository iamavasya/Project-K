using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Dtos.AuthModule
{
    public class JwtResponse
    {
        public string AccessToken { get; set; } = null!;
        public RefreshToken RefreshToken { get; set; } = null!;
    }
}
