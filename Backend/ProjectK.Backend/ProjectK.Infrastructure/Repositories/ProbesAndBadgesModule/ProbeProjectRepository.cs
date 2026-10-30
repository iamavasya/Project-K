using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.ProbesAndBadgesModule;

public class ProbeProjectRepository : BaseEntityRepository<ProbeProject>, IProbeProjectRepository
{
    public ProbeProjectRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ProbeProject>> GetForMemberProbeAsync(Guid memberKey, string probeId, CancellationToken cancellationToken = default)
    {
        return await Context.ProbeProjects
            .AsTracking()
            .Include(project => project.Images)
            .Where(project => project.MemberKey == memberKey && project.ProbeId == probeId)
            .OrderBy(project => project.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProbeProject?> GetWithImagesAsync(Guid projectKey, CancellationToken cancellationToken = default)
    {
        return await Context.ProbeProjects
            .AsTracking()
            .Include(project => project.Images)
            .FirstOrDefaultAsync(project => project.ProbeProjectKey == projectKey, cancellationToken);
    }

    public void AddImage(ProbeProjectImage image) => Context.ProbeProjectImages.Add(image);

    public void RemoveImages(IEnumerable<ProbeProjectImage> images) => Context.ProbeProjectImages.RemoveRange(images);

    public async Task<IReadOnlyList<string>> GetAllImageBlobNamesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.ProbeProjectImages
            .AsNoTracking()
            .Select(image => image.BlobName)
            .ToListAsync(cancellationToken);
    }

    public override Task<IEnumerable<ProbeProject>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use GetForMemberProbeAsync instead.");
    }

    public async Task DeleteForMembersAsync(IReadOnlyCollection<Guid> memberKeys, CancellationToken cancellationToken = default)
    {
        if (memberKeys.Count == 0)
        {
            return;
        }

        // Through the tracker, like the other progress rows: an out-of-band delete would commit
        // ahead of the use case that asked for it. Pictures follow by cascade.
        var rows = await Context.ProbeProjects
            .Where(project => memberKeys.Contains(project.MemberKey))
            .ToListAsync(cancellationToken);

        Context.ProbeProjects.RemoveRange(rows);
    }
}
