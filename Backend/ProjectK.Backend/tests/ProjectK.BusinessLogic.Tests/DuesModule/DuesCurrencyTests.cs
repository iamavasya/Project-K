using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.DuesModule.Services;
using ProjectK.Common.Interfaces.Modules.KraiModule;
using ProjectK.Common.Models.KraiProfiles;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.DuesModule;

public class DuesCurrencyTests
{
    private static readonly Guid Kurin = Guid.NewGuid();

    [Fact]
    public async Task AKurin_CountsItsDues_InItsKraisCurrency()
    {
        var profiles = new Mock<IKraiProfileProvider>();
        profiles.Setup(p => p.GetForKurinAsync(Kurin, It.IsAny<CancellationToken>())).ReturnsAsync(new KraiProfile(
            "cz", "Чехія", 1, new KraiCatalogSettings(["cz"], []), new KraiLocaleSettings("CZK", "Europe/Prague", "uk-UA")));

        (await profiles.Object.CurrencyForKurinAsync(Kurin, default)).Should().Be("CZK");
    }

    [Fact]
    public async Task AnUnknownKurin_CountsInHryvnias()
    {
        (await new Mock<IKraiProfileProvider>().Object.CurrencyForKurinAsync(Kurin, default)).Should().Be("UAH");
    }
}
