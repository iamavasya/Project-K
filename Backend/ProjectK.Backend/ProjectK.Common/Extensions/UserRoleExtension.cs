using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Extensions
{
    public static class UserRoleExtension
    {
        public static string ToClaimValue(this UserRole role) => role.ToString();

        public static UserRole ToUserRole(this string role) => 
            Enum.TryParse<UserRole>(role, out var parsed) 
                ? parsed 
                : throw new ArgumentException($"Invalid role {role}");
    }
}
