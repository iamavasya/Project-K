using ProjectK.Common.Models.Dtos.AuthModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule
{
    public interface IJwtService
    {
        string GenerateAccessToken(string userId, string email, IEnumerable<string> roles, string? kurinKey);
        RefreshToken GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
