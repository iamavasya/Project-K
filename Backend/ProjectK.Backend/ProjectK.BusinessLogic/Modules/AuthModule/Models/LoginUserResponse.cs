using System.Text.Json.Serialization;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Models;

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

    /// <summary>
    /// Proof that the password step passed, handed back only when <see cref="RequiresMfa"/> is
    /// set. The second-factor step has to bring it back; without it a code alone is refused.
    /// </summary>
    public string? MfaToken { get; set; }

    public JwtResponse? Tokens { get; set; } = null!;

    /// <summary>
    /// Issued when the second factor just passed. The controller turns it into an HttpOnly cookie
    /// and it never reaches the JSON body: a script must not be able to read what makes a device trusted.
    /// </summary>
    [JsonIgnore]
    public MfaTrustGrant? MfaTrust { get; set; }
}
