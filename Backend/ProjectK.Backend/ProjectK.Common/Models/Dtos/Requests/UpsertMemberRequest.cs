using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Dtos.Requests
{
    public class UpsertMemberRequest
    {
        public Guid? MemberKey { get; set; }
        public Guid GroupKey { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public bool? RemoveProfilePhoto { get; set; }
        public IFormFile? Blob { get; set; }
    }
}
