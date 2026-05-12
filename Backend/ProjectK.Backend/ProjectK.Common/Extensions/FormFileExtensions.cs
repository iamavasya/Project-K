using Microsoft.AspNetCore.Http;

namespace ProjectK.Common.Extensions
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
