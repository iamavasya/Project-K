using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Dtos.Requests
{
    public class CreateMemberRequest
    {
        [JsonPropertyName("groupKey")]
        public Guid GroupKey { get; set; }
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }
        [JsonPropertyName("middleName")]
        public string MiddleName { get; set; }
        [JsonPropertyName("lastName")]
        public string LastName { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
        [JsonPropertyName("dateOfBirth")]
        public DateOnly DateOfBirth { get; set; }
    }
}
