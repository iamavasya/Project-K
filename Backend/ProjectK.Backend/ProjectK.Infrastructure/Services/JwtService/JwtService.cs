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
    // TODO: Продовжити розбирати сервіс
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Генерує JWT access token
        /// </summary>
        public string GenerateAccessToken(string userId, string email, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email)
            };

            // Додаємо ролі як окремі claim
            claims.AddRange(roles.Select(role => new Claim("roles", role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiresInMinutes"])),
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
                Expires = DateTime.UtcNow.AddDays(7), // приклад: 7 днів
                Created = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Декодує прострочений токен (для refresh flow)
        /// </summary>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // Ігноруємо термін життя
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"])),
                RoleClaimType = "roles",
                NameClaimType = JwtRegisteredClaimNames.Sub
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    throw new SecurityTokenException("Invalid token");

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
