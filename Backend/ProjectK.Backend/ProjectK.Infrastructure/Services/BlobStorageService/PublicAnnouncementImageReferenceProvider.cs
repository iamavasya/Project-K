using Microsoft.EntityFrameworkCore;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Services.BlobStorageService
{
    public sealed class PublicAnnouncementImageReferenceProvider : IPhotoReferenceProvider
    {
        private readonly AppDbContext _db;

        public PublicAnnouncementImageReferenceProvider(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyCollection<string>> GetAllReferencedBlobNamesAsync(CancellationToken cancellationToken)
        {
            var imageBlobKeys = await _db.PublicAnnouncementDrafts
                .AsNoTracking()
                .Where(a => a.ImageBlobKey != null && a.ImageBlobKey != "")
                .Select(a => a.ImageBlobKey!)
                .ToListAsync(cancellationToken);

            return imageBlobKeys
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }
    }
}
