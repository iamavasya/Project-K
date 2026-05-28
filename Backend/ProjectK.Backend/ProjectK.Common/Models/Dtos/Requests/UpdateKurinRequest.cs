using System.ComponentModel.DataAnnotations;

namespace ProjectK.Common.Models.Dtos.Requests
{
    public class UpdateKurinRequest
    {
        public int Number { get; set; }

        [MaxLength(120)]
        public string? Stanytsia { get; set; }

        [MaxLength(120)]
        public string? RegionOrCountry { get; set; }

        [MaxLength(200)]
        public string? NamedAfter { get; set; }

        [MaxLength(4000)]
        public string? Description { get; set; }
    }
}
