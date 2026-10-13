using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Settings;
using ProjectK.Infrastructure.Services.Catalog;
using Xunit;

namespace ProjectK.Infrastructure.Tests.Services.Catalog;

public sealed class CatalogSyncBackgroundServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "projectk-catalog-tests", Guid.NewGuid().ToString("N"));
    private readonly StubHandler _handler = new();
    private readonly CatalogStore _store = new();

    private string SnapshotPath => Path.Combine(_directory, "catalog.json");

    private CatalogSyncBackgroundService Service(string? krai = null)
    {
        var options = Options.Create(new CatalogOptions { BaseUrl = "https://catalog.test/", Krai = krai, SnapshotPath = SnapshotPath });
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(CatalogClient.HttpClientName)).Returns(() => new HttpClient(_handler, disposeHandler: false));
        return new CatalogSyncBackgroundService(_store, new CatalogClient(factory.Object, options), options, NullLogger<CatalogSyncBackgroundService>.Instance);
    }

    [Fact]
    public async Task Start_WithoutACopy_FetchesAndSavesOne()
    {
        _handler.Respond(HttpStatusCode.OK, CatalogTestData.Bytes, "\"r7\"");
        var service = Service(krai: "ua");

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal("https://catalog.test/v1/catalog?krai=ua", _handler.Requests[0].RequestUri!.ToString());
        Assert.Equal(2, ((IBadgesCatalog)_store).GetAll().Count);
        Assert.True(File.Exists(SnapshotPath));
        Assert.Equal("\"r7\"", File.ReadAllText(SnapshotPath + ".etag"));
    }

    [Fact]
    public async Task Start_WithACopy_ServesItWhenTheServiceIsDown()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllBytes(SnapshotPath, CatalogTestData.Bytes);
        _handler.Respond(HttpStatusCode.ServiceUnavailable);
        var service = Service();

        await service.StartAsync(CancellationToken.None);
        var refreshed = await service.RefreshAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.False(refreshed);
        Assert.Equal(7, _store.State.Revision);
        Assert.NotNull(((IProbesCatalog)_store).GetById("probe-1"));
    }

    [Fact]
    public async Task Refresh_SendsTheETag_AndKeepsTheCatalogueOn304()
    {
        _handler.Respond(HttpStatusCode.OK, CatalogTestData.Bytes, "\"r7\"");
        var service = Service();
        await service.RefreshAsync(CancellationToken.None);

        _handler.Respond(HttpStatusCode.NotModified);
        var refreshed = await service.RefreshAsync(CancellationToken.None);

        Assert.True(refreshed);
        Assert.Equal("\"r7\"", _handler.Requests[1].Headers.IfNoneMatch.Single().ToString());
        Assert.Equal(7, _store.State.Revision);
    }

    [Fact]
    public async Task Refresh_KeepsTheCatalogue_WhenTheAnswerIsEmpty()
    {
        _handler.Respond(HttpStatusCode.OK, CatalogTestData.Bytes);
        var service = Service();
        await service.RefreshAsync(CancellationToken.None);

        _handler.Respond(HttpStatusCode.OK, """{ "schemaVersion": 2, "revision": 8, "categories": [], "badges": [], "probes": [], "awards": [] }"""u8.ToArray());
        var refreshed = await service.RefreshAsync(CancellationToken.None);

        Assert.False(refreshed);
        Assert.Equal(7, _store.State.Revision);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private Func<HttpResponseMessage> _next = () => new HttpResponseMessage(HttpStatusCode.NotFound);

        public List<HttpRequestMessage> Requests { get; } = [];

        public void Respond(HttpStatusCode status, byte[]? body = null, string? etag = null)
        {
            _next = () =>
            {
                var response = new HttpResponseMessage(status) { Content = new ByteArrayContent(body ?? []) };
                if (etag is not null)
                {
                    response.Headers.ETag = EntityTagHeaderValue.Parse(etag);
                }

                return response;
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_next());
        }
    }
}
