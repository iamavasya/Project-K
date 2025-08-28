using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Helpers
{
    public static class ReadFileHelperFunction
    {
        public static async Task<byte[]?> ReadFileAsync(IFormFile? file, CancellationToken ct)
        {
            if (file == null || file.Length == 0) return null;
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
    }
}
