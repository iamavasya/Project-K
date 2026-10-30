using Microsoft.EntityFrameworkCore;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Services.BlobStorageService;

/// <summary>
/// The pictures of third-probe projects. A picture removed from a description loses its row on
/// save, and a removed project or person takes theirs along; what is left without a row is swept.
/// </summary>
public sealed class ProbeProjectImageReferenceProvider : IPhotoReferenceProvider
{
    private readonly AppDbContext _db;

    public ProbeProjectImageReferenceProvider(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<string>> GetAllReferencedBlobNamesAsync(CancellationToken cancellationToken)
    {
        return await _db.ProbeProjectImages
            .AsNoTracking()
            .Select(image => image.BlobName)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
