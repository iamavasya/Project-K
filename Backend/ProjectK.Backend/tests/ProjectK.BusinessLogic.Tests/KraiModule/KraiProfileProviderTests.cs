using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectK.BusinessLogic.Modules.KraiModule.Features.Profile;
using ProjectK.BusinessLogic.Modules.KraiModule.Services;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.BusinessLogic.Services.Caching;
using ProjectK.Common.Entities.KraiModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KraiModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KraiModule;

public class KraiProfileProviderTests
{
    private readonly Mock<IKraiRepository> _krais = new();
    private readonly Mock<IKurinRepository> _kurins = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly KraiProfileProvider _provider;

    public KraiProfileProviderTests()
    {
        var cache = new Mock<IBackendCache>();
        cache.Setup(c => c.GetOrCreateAsync(
                It.IsAny<CachePolicy>(),
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<Common.Models.KraiProfiles.KraiProfile?>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<CacheScopeContext?>()))
            .Returns((CachePolicy _, string _, Func<CancellationToken, Task<Common.Models.KraiProfiles.KraiProfile?>> factory, CancellationToken token, CacheScopeContext? _) => factory(token));

        _unitOfWork.Setup(u => u.Krais).Returns(_krais.Object);
        _unitOfWork.Setup(u => u.Kurins).Returns(_kurins.Object);
        _provider = new KraiProfileProvider(_unitOfWork.Object, cache.Object, NullLogger<KraiProfileProvider>.Instance);
    }

    private void Given(string code, string? json, int revision = 1)
    {
        _krais.Setup(r => r.GetAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Krai { Code = code, Name = code.ToUpperInvariant() });
        _krais.Setup(r => r.GetLatestProfileRevisionAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(json is null ? null : new KraiProfileRevision { KraiCode = code, Revision = revision, Json = json });
    }

    [Fact]
    public async Task AnEmptyProfile_GivesTheValuesInCode()
    {
        Given("ua", "{}");

        var profile = await _provider.GetForKraiAsync("ua");

        profile!.Revision.Should().Be(1);
        profile.Catalog.BadgeSources.Should().Equal("ua");
        profile.Catalog.ProbeSources.Should().Equal("ua");
        profile.Catalog.HiddenBadges.Should().BeEmpty();
        profile.Catalog.HiddenProbes.Should().BeEmpty();
        profile.Locale.Currency.Should().Be("UAH");
        profile.Locale.TimeZone.Should().Be(AgendaRecurrence.DefaultZone.Id);
        profile.Locale.NumberLocale.Should().Be("uk-UA");
    }

    [Fact]
    public async Task SavedValues_OverrideTheValuesInCode()
    {
        Given("cz", """
            {
              "catalog": { "badgeSources": [" CZ ", "ua", "cz"], "probeSources": [], "hiddenBadges": [" b1 ", "", "b1"] },
              "locale": { "currency": "czk", "timeZone": "Europe/Prague" }
            }
            """, revision: 3);

        var profile = await _provider.GetForKraiAsync("cz");

        profile!.Revision.Should().Be(3);
        profile.Catalog.BadgeSources.Should().Equal("cz", "ua");
        profile.Catalog.ProbeSources.Should().Equal("cz");
        profile.Catalog.HiddenBadges.Should().Equal("b1");
        profile.Locale.Currency.Should().Be("CZK");
        profile.Locale.TimeZone.Should().Be("Europe/Prague");
        profile.Locale.NumberLocale.Should().Be("uk-UA");
    }

    [Fact]
    public async Task BrokenJson_FallsBackToTheValuesInCode()
    {
        Given("cz", "{ not json");

        var profile = await _provider.GetForKraiAsync("cz");

        profile!.Catalog.BadgeSources.Should().Equal("cz");
        profile.Locale.Currency.Should().Be("UAH");
    }

    [Fact]
    public async Task AKraiWithoutRevisions_HasRevisionZero()
    {
        Given("cz", null);

        var profile = await _provider.GetForKraiAsync("cz");

        profile!.Revision.Should().Be(0);
        profile.Catalog.BadgeSources.Should().Equal("cz");
    }

    [Fact]
    public async Task AnUnknownKrai_HasNoProfile()
    {
        (await _provider.GetForKraiAsync("xx")).Should().BeNull();
    }

    [Fact]
    public async Task AKurin_GetsTheProfileOfItsKrai()
    {
        Given("cz", "{}");
        var kurin = new Kurin(5) { KraiCode = "cz" };
        _kurins.Setup(r => r.GetByKeyAsync(kurin.KurinKey, It.IsAny<CancellationToken>())).ReturnsAsync(kurin);

        var result = await new GetKurinProfileQueryHandler(_provider).Handle(new GetKurinProfileQuery(kurin.KurinKey), default);

        result.Type.Should().Be(ResultType.Success);
        result.Data!.KraiCode.Should().Be("cz");
    }

    [Fact]
    public async Task AMissingKurin_IsNotFound()
    {
        var result = await new GetKurinProfileQueryHandler(_provider).Handle(new GetKurinProfileQuery(Guid.NewGuid()), default);

        result.Type.Should().Be(ResultType.NotFound);
        result.ErrorCode.Should().Be("KurinNotFound");
    }
}
