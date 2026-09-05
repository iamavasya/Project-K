using System.ComponentModel.DataAnnotations;

namespace ProjectK.Common.Models.Dtos.KurinModule.Requests
{
    public sealed class VerifyMemberProfileRequest
    {
        [MaxLength(1000)]
        public string? Note { get; set; }
    }
}
