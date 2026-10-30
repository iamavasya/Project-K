using ProjectK.Common.Entities.ProbesAndBadgesModule;

namespace ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;

public interface IProbeProjectRepository : IBaseEntityRepository<ProbeProject>
{
    /// <summary>One person's projects for one probe, oldest first, with their pictures; tracked.</summary>
    Task<IReadOnlyList<ProbeProject>> GetForMemberProbeAsync(Guid memberKey, string probeId, CancellationToken cancellationToken = default);

    /// <summary>One project with its pictures, tracked.</summary>
    Task<ProbeProject?> GetWithImagesAsync(Guid projectKey, CancellationToken cancellationToken = default);

    void AddImage(ProbeProjectImage image);

    void RemoveImages(IEnumerable<ProbeProjectImage> images);

    /// <summary>Every blob a project picture lives in: what the orphan cleanup must leave alone.</summary>
    Task<IReadOnlyList<string>> GetAllImageBlobNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes these people's projects and pictures. No foreign key ties them to the member row;
    /// the blobs go with the next orphan sweep.
    /// </summary>
    Task DeleteForMembersAsync(IReadOnlyCollection<Guid> memberKeys, CancellationToken cancellationToken = default);
}
