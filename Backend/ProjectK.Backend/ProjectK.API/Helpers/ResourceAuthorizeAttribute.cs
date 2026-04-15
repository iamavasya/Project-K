using Microsoft.AspNetCore.Mvc;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Helpers;

public sealed class ResourceAuthorizeAttribute : TypeFilterAttribute
{
    public ResourceAuthorizeAttribute(ResourceType resourceType, ResourceAction action, string resourceKeySelector)
        : base(typeof(ResourceAuthorizeFilter))
    {
        Arguments = [true, resourceType, string.Empty, action, resourceKeySelector];
    }

    public ResourceAuthorizeAttribute(string resourceTypeSelector, ResourceAction action, string resourceKeySelector)
        : base(typeof(ResourceAuthorizeFilter))
    {
        Arguments = [false, default(ResourceType), resourceTypeSelector, action, resourceKeySelector];
    }
}