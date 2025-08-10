using Microsoft.AspNetCore.Mvc;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Extensions
{
    public static class ServiceResultExtension
    {
        public static IActionResult ToActionResult<T>(this ServiceResult<T> result, ControllerBase controller)
        {
            return result.Type switch
            {
                ResultType.Success => controller.Ok(result.Data),
                ResultType.Created => result.CreatedAtActionName != null
                    ? controller.CreatedAtAction(result.CreatedAtActionName, result.CreatedAtRouteValues, result.Data)
                    : controller.Created(string.Empty, result.Data),
                ResultType.BadRequest => controller.BadRequest(result.Data),
                ResultType.Unauthorized => controller.Unauthorized(),
                ResultType.NotFound => controller.NotFound(result.Data),
                ResultType.Conflict => controller.Conflict(new object[] { "The entity that was attempted to be created already exists.", result.Data! }),
                // ResultType.Forbidden => controller.Forbid(),
                _ => controller.StatusCode(500, "An unexpected error occurred."),
            };
        }
    }
}
