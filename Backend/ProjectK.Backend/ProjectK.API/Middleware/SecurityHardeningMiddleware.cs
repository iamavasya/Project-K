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

        // 2. Tailscale Restriction for Admins/Managers
        if (_options.Value.EnableTailscaleOnlyForAdmins)
        {
            var user = context.User;
            var isPrivileged = user.IsInRole(UserRole.Admin.ToClaimValue()) || 
                             user.IsInRole(UserRole.Manager.ToClaimValue());

            if (isPrivileged)
            {
                var ipAddr = context.Connection.RemoteIpAddress!;
                if (!IsIpInTailscaleRange(ipAddr, _options.Value.TailscaleIpRange))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Administrative access is only allowed via Tailscale.");
                    return;
                }
            }
        }
        
        await _next(context);
    }

    private bool IsIpInTailscaleRange(IPAddress ip, string cidr)
    {
        if (ip.AddressFamily != AddressFamily.InterNetwork) return false;

        try 
        {
            string[] parts = cidr.Split('/');
            if (parts.Length != 2) return false;

            var networkAddress = IPAddress.Parse(parts[0]);
            int maskLength = int.Parse(parts[1]);

            uint ipAddress = BitConverter.ToUInt32(ip.GetAddressBytes().Reverse().ToArray(), 0);
            uint networkAddr = BitConverter.ToUInt32(networkAddress.GetAddressBytes().Reverse().ToArray(), 0);
            uint mask = uint.MaxValue << (32 - maskLength);

            return (ipAddress & mask) == (networkAddr & mask);
        }
        catch
        {
            return false;
        }
    }
}
