using ProjectK.Common.Models.Records;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule
{
    public interface IPhotoService
    {
        // Stream overloads let callers feed the request/file stream straight into image processing
        // without first buffering the whole original file into a byte[] in memory.
        Task<PhotoUploadResult> UploadPhotoAsync(Stream photoStream, string fileName, CancellationToken cancellationToken);
        Task<PhotoUploadResult> UploadPhotoAsync(Stream photoStream, string fileName, BlobUploadContext uploadContext, CancellationToken cancellationToken);
        Task<PhotoUploadResult> UploadPhotoAsync(byte[] photoBytes, string fileName, CancellationToken cancellationToken);
        Task<PhotoUploadResult> UploadPhotoAsync(byte[] photoBytes, string fileName, BlobUploadContext uploadContext, CancellationToken cancellationToken);
        Task<bool> DeletePhotoAsync(string photoUrl, CancellationToken cancellationToken);
    }
}
