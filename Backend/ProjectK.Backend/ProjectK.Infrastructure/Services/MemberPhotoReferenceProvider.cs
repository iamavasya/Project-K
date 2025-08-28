using Microsoft.EntityFrameworkCore;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Services
{
    public class MemberPhotoReferenceProvider : IPhotoReferenceProvider
    {
        private readonly AppDbContext _db;
        public MemberPhotoReferenceProvider(AppDbContext db) => _db = db;

        public async Task<IReadOnlyCollection<string>> GetAllReferencedBlobNamesAsync(CancellationToken cancellationToken)
        {
            return await _db.Members
                .Where(m => m.ProfilePhotoBlobName != null && m.ProfilePhotoBlobName != "")
                .Select(m => m.ProfilePhotoBlobName!)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
    }
}
