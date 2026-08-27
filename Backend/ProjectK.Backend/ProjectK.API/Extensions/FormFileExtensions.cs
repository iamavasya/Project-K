using Microsoft.AspNetCore.Http;
using ProjectK.API.Extensions;

namespace ProjectK.API.Extensions
{
    public static class FormFileExtensions
    {
        public static async Task<byte[]?> ToByteArrayAsync(this IFormFile? file, CancellationToken ct = default)
        {
            if (file == null || file.Length == 0) return null;
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
    }
}
