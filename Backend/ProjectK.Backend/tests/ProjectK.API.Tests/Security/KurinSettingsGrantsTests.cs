using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Tests.Security;

/// <summary>
/// Who opens the kurin's settings: the whole КВ reads them, «Права й видимість» included; only the
/// Звʼязковий changes them. The rest of the kurin, провід included, does not read them at all.
/// </summary>
public class KurinSettingsGrantsTests
{
    private static AccessScope? Widest(string role, ResourceType resource, ResourceAction action) =>
        RolePermissionMap.WidestScope(RolePermissionMap.Resolve([role]), resource, action);

    [Theory]
    [InlineData("KV.Zvyazkovyi")]
    [InlineData("KV.Vykhovnyk")]
    [InlineData("KV.Instruktor")]
    public void TheKV_ReadsTheSettings(string role)
    {
        Assert.Equal(AccessScope.KurinWide, Widest(role, ResourceType.KurinSettings, ResourceAction.Read));
    }

    [Theory]
    [InlineData("KV.Vykhovnyk")]
    [InlineData("KV.Instruktor")]
    public void OnlyTheZvyazkovyi_ChangesThem(string role)
    {
        Assert.Null(Widest(role, ResourceType.Kurin, ResourceAction.Update));
        Assert.Equal(AccessScope.KurinWide, Widest("KV.Zvyazkovyi", ResourceType.Kurin, ResourceAction.Update));
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Kurin.Kurinnuy")]
    [InlineData("Group.Hurtkoviy")]
    [InlineData("Kurin.Skarbnyk")]
    public void TheRest_DoNotRead(string role)
    {
        Assert.Null(Widest(role, ResourceType.KurinSettings, ResourceAction.Read));
    }
}
