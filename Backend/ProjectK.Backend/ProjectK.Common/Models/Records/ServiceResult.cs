using Microsoft.AspNetCore.Mvc;
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
        object? CreatedAtRouteValues = null)
    {
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        public static ServiceResult<T> Failure(ResultType type, string errorCode, string errorMessage)
        {
            return new ServiceResult<T>(type)
            {
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }
}
