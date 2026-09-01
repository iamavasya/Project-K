using System.Text.Json;

namespace ProjectK.API.Tests.Security;

/// <summary>
/// <c>SecurityPatch:EnableResourceGuard</c> switches off <c>ResourceAuthorizeFilter</c> entirely —
/// the filter's one path that calls the action without deciding anything.
/// <para>
/// It used to matter less, because most endpoints also carried a coarse policy that would still
/// refuse. Those policies are gone wherever the resource check already made the same decision, so on
/// those endpoints this switch is now the difference between "the office that owns this record" and
/// "anyone signed in". Shipping with it off is not a degraded mode; it is an open door.
/// </para>
/// </summary>
public class ResourceGuardSwitchTests
{
    [Fact]
    public void EveryEnvironment_ShouldShipWithTheResourceGuardOn()
    {
        var settings = ApiSettingsFiles().ToList();
        Assert.NotEmpty(settings);

        var offences = new List<string>();

        foreach (var file in settings)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));

            if (!document.RootElement.TryGetProperty("SecurityPatch", out var section)
                || !section.TryGetProperty("EnableResourceGuard", out var enabled))
            {
                // Inherited from appsettings.json, which this test also checks.
                continue;
            }

            if (enabled.ValueKind != JsonValueKind.True)
            {
                offences.Add(Path.GetFileName(file));
            }
        }

        Assert.True(offences.Count == 0,
            "Resource guard disabled in: " + string.Join(", ", offences));
    }

    [Fact]
    public void TheBaseSettings_ShouldTurnTheResourceGuardOn()
    {
        var baseSettings = ApiSettingsFiles()
            .Single(file => Path.GetFileName(file) == "appsettings.json");

        using var document = JsonDocument.Parse(File.ReadAllText(baseSettings));

        Assert.True(document.RootElement
            .GetProperty("SecurityPatch")
            .GetProperty("EnableResourceGuard")
            .GetBoolean());
    }

    private static IEnumerable<string> ApiSettingsFiles()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "ProjectK.API")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);

        return Directory.EnumerateFiles(
            Path.Combine(directory!.FullName, "ProjectK.API"), "appsettings*.json");
    }
}
