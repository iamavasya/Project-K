using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Extensions;

public static class UserRoleExtension
{
    public static string ToClaimValue(this UserRole role) => role.ToString();
}
