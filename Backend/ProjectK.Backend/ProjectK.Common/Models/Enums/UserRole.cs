using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Enums
{
    // System-level roles only. Kurin authority is no longer modelled here — it comes from
    // діловодські offices (see ProjectK.Common.Models.Authorization.SystemRole / RolePermissionMap).
    public enum UserRole
    {
        Admin,
        Member
    }
}
