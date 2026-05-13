using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ProjectK.API.Helpers;
using ProjectK.Common.Models.Enums;
using System.Security.Claims;
using ProjectK.Common.Extensions;
using ProjectK.API.Services;

namespace ProjectK.API.Middleware;

public sealed class SecurityHardeningMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<SecurityPatchOptions> _options;

    public SecurityHardeningMiddleware(RequestDelegate next, IOptions<SecurityPatchOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context, GeoIPService geoIPService)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        
        if (remoteIp == null)
        {
            await _next(context);
            return;
        }

        // 1. Geo-blocking (RU/BY check)
        if (_options.Value.EnableGeoBlocking)
        {
            var countryCode = await geoIPService.GetCountryCodeAsync(remoteIp);
            if (countryCode != null && _options.Value.BlockedCountries.Contains(countryCode, StringComparer.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access from your region is restricted.");
                return;
            }
        }

        await _next(context);
    }
}
