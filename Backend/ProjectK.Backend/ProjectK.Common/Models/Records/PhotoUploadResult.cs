using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Records
{
    public sealed record PhotoUploadResult(string BlobName, string Url);
}
