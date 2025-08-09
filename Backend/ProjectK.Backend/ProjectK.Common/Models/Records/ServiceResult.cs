using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Records
{
    public record ServiceResult<T>(
        ResultType Type,
        T? Data = default,
        string? CreatedAtActionName = null,
        object? CreatedAtRouteValues = null);

}
