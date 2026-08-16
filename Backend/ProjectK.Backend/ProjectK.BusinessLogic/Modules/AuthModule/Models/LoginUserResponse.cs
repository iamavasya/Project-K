using ProjectK.Common.Models.Dtos.AuthModule;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Models
{
    public class LoginUserResponse
    {
        public Guid UserKey { get; set; }
        public Guid? MemberKey { get; set; }
        public string Email { get; set; } = null!;
        public bool IsAdmin { get; set; }
        public IReadOnlyCollection<string> Permissions { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
        public string? KurinKey { get; set; }
        public bool RequiresMfa { get; set; }
        public JwtResponse? Tokens { get; set; } = null!;
    }
}
