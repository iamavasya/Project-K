using ProjectK.BusinessLogic.Modules.KurinModule.Models;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Services;

/// <summary>Whose tags the viewer sees, and the kurin's tag switches. See <see cref="KurinTagVisibility"/>.</summary>
public interface IKurinTagVisibility
{
    /// <summary>The kurin's switches with its defaults filled in; null when there is no such kurin.</summary>
    Task<KurinTagSettingsState?> SettingsAsync(Guid kurinKey, CancellationToken cancellationToken = default);

    /// <summary>Whether the viewer may define and put on tags in the kurin.</summary>
    Task<bool> CanManageAsync(Guid kurinKey, CancellationToken cancellationToken = default);

    /// <summary>Fills the tags the viewer may see on people seen in the kurin.</summary>
    Task FillAsync(Guid kurinKey, IReadOnlyCollection<MemberResponse> rows, CancellationToken cancellationToken = default);
}
