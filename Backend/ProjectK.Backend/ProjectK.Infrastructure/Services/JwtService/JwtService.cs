using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.AuthModule;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Services.JwtService
{
    public class JwtService : IJwtService
    {
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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

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

    }
}
