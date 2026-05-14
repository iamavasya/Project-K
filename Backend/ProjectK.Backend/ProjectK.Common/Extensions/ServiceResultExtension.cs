using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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
            if (result.ErrorCode != null || result.ErrorMessage != null)
            {
                var errorResponse = new { error = result.ErrorCode, message = result.ErrorMessage };
                return result.Type switch
                {
                    ResultType.BadRequest => controller.BadRequest(errorResponse),
                    ResultType.Unauthorized => controller.StatusCode(StatusCodes.Status401Unauthorized, errorResponse),
                    ResultType.NotFound => controller.NotFound(errorResponse),
                    ResultType.Conflict => controller.Conflict(errorResponse),
                    ResultType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, errorResponse),
                    _ => controller.StatusCode(StatusCodes.Status500InternalServerError, errorResponse)
                };
            }

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
                ResultType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, result.Data),
                _ => controller.StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
            };
        }
    }
}
