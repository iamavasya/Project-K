using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule
{
    public interface IPhotoService
    {
        Task<PhotoUploadResult> UploadPhotoAsync(byte[] photoBytes, string fileName, CancellationToken cancellationToken);
        Task<bool> DeletePhotoAsync(string photoUrl, CancellationToken cancellationToken);
        Task<IEnumerable<string>> GetOrphanFilesAsync(CancellationToken cancellationToken);
    }
}
