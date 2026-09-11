using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.AuthModule;

namespace ProjectK.Infrastructure.Services.JwtService;

public class JwtService : IJwtService
{
    private const string MfaChallengePurposeClaim = "purpose";
    private const string MfaChallengePurpose = "mfa-challenge";
    private static readonly TimeSpan MfaChallengeLifetime = TimeSpan.FromMinutes(5);

    private readonly IConfiguration _config;
    private readonly TimeProvider _timeProvider;

    public JwtService(IConfiguration config, TimeProvider timeProvider)
    {
        _config = config;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Генерує JWT access token
    /// </summary>
    public string GenerateAccessToken(string userId, string email, IEnumerable<string> roles, string? kurinKey)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email)
        };

        // Add other claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        if (kurinKey != null)
        {
            claims.Add(new Claim("kurinKey", kurinKey));
        }

        var creds = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(int.Parse(_config["Jwt:ExpiresInMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Генерує refresh token
    /// </summary>
    public RefreshToken GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            Expires = _timeProvider.GetUtcNow().UtcDateTime.AddDays(int.Parse(_config["Jwt:RefreshTokenExpiresInDays"])),
            Created = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public string GenerateMfaChallengeToken(Guid userId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(MfaChallengePurposeClaim, MfaChallengePurpose)
        };

        // Its own audience, so the bearer middleware never accepts a challenge as an access
        // token — the two are signed with the same key and would otherwise look alike.
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: MfaChallengeAudience,
            claims: claims,
            expires: _timeProvider.GetUtcNow().UtcDateTime.Add(MfaChallengeLifetime),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public Guid? ReadMfaChallenge(string token)
    {
        // Claims are read as written: the default handler renames "sub" to NameIdentifier on the
        // way in, which is a surprise nobody needs here.
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = MfaChallengeAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SigningKey,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            if (principal.FindFirst(MfaChallengePurposeClaim)?.Value != MfaChallengePurpose)
            {
                return null;
            }

            return Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)
                ? userId
                : null;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

    private string MfaChallengeAudience => $"{_config["Jwt:Audience"]}:{MfaChallengePurpose}";
}
