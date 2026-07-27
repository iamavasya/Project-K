using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectK.API.Services;

namespace ProjectK.API.Tests.Security;

public class GeoIPServiceTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("172.18.0.1")]      // Docker bridge gateway
    [InlineData("10.1.2.3")]
    [InlineData("192.168.1.10")]
    [InlineData("100.64.66.7")]     // CGNAT, used by Tailscale
    [InlineData("169.254.10.1")]    // link-local
    [InlineData("::ffff:172.18.0.1")]
    [InlineData("not-an-ip")]
    public async Task GetCountryCodeAsync_ShouldSkipLookup_ForNonRoutableAddresses(string ip)
    {
        var handler = new CountingHandler(() => JsonResponse("{\"status\":\"success\",\"countryCode\":\"UA\"}"));
        var service = CreateService(handler);

        var result = await service.GetCountryCodeAsync(ip);

        result.Should().Be("LOCAL");
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task GetCountryCodeAsync_ShouldResolveCountry_ForPublicAddress()
    {
        var handler = new CountingHandler(() => JsonResponse("{\"status\":\"success\",\"countryCode\":\"UA\"}"));
        var service = CreateService(handler);

        var result = await service.GetCountryCodeAsync("8.8.8.8");

        result.Should().Be("UA");
        handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task GetCountryCodeAsync_ShouldNotRepeatLookup_WhenProviderThrottles()
    {
        var handler = new CountingHandler(() => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var service = CreateService(handler);

        var first = await service.GetCountryCodeAsync("8.8.8.8");
        var second = await service.GetCountryCodeAsync("8.8.8.8");

        first.Should().BeNull();
        second.Should().BeNull();
        handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task GetCountryCodeAsync_ShouldGiveUp_WhenProviderHangs()
    {
        var handler = new CountingHandler(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return JsonResponse("{\"status\":\"success\",\"countryCode\":\"UA\"}");
        });
        var service = CreateService(handler);

        var result = await service.GetCountryCodeAsync("8.8.8.8");

        result.Should().BeNull();
    }

    private static GeoIPService CreateService(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<GeoIPService>.Instance);

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly Func<CancellationToken, Task<HttpResponseMessage>> _respond;

        public CountingHandler(Func<HttpResponseMessage> respond)
            : this(_ => Task.FromResult(respond()))
        {
        }

        public CountingHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond)
        {
            _respond = respond;
        }

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return _respond(cancellationToken);
        }
    }
}
