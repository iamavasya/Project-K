using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProjectK.API.Swagger;

/// <summary>
/// Documents the error contract once instead of on every action.
/// <para>
/// Every failure this API returns is <c>{ "error": "&lt;StableCode&gt;", "message": "…" }</c>, so
/// repeating five <c>[ProducesResponseType]</c> attributes per action would be a hundred lines of
/// noise that drift out of date. Actions still declare their own success type.
/// </para>
/// </summary>
public sealed class UnifiedErrorResponsesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        AddError(operation, "400", "The request was rejected.");
        AddError(operation, "500", "Something failed unexpectedly.");

        var allowsAnonymous = context.MethodInfo
            .GetCustomAttributes(inherit: true)
            .Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true) ?? [])
            .OfType<AllowAnonymousAttribute>()
            .Any();

        if (allowsAnonymous)
        {
            return;
        }

        AddError(operation, "401", "The caller is not authenticated, or the token carries no readable identity.");
        AddError(operation, "403", "The caller is authenticated but not permitted on this resource.");
    }

    private static void AddError(OpenApiOperation operation, string statusCode, string description)
    {
        operation.Responses ??= new OpenApiResponses();
        if (operation.Responses.ContainsKey(statusCode))
        {
            return;
        }

        operation.Responses[statusCode] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["error"] = new OpenApiSchema { Type = JsonSchemaType.String },
                            ["message"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                }
            }
        };
    }
}
