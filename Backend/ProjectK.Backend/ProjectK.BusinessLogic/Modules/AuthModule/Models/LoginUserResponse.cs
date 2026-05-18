using ProjectK.Common.Models.Dtos.AuthModule;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Models
{
    public class LoginUserResponse
    {
        public Guid UserKey { get; set; }
        public Guid? MemberKey { get; set; }
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? KurinKey { get; set; }
        public bool RequiresMfa { get; set; }
        public JwtResponse? Tokens { get; set; } = null!;
    }
}
