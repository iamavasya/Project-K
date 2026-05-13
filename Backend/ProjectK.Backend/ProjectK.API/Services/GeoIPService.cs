using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ProjectK.API.Services;

public sealed class GeoIPService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeoIPService> _logger;

    public GeoIPService(HttpClient httpClient, IMemoryCache cache, ILogger<GeoIPService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetCountryCodeAsync(string ip)
    {
        if (string.IsNullOrEmpty(ip) || ip == "::1" || ip == "127.0.0.1")
            return "LOCAL";

        if (_cache.TryGetValue(ip, out string? cachedCountry))
            return cachedCountry;

        try
        {
            // Using ip-api.com (free for non-commercial use, 45 requests/min)
            var response = await _httpClient.GetFromJsonAsync<IpApiResponse>($"http://ip-api.com/json/{ip}?fields=status,countryCode");

            if (response?.status == "success" && !string.IsNullOrEmpty(response.countryCode))
            {
                _cache.Set(ip, response.countryCode, TimeSpan.FromDays(1));
                return response.countryCode;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching GeoIP data for {IP}", ip);
        }

        return null;
    }

    private record IpApiResponse(string status, string countryCode);
}
