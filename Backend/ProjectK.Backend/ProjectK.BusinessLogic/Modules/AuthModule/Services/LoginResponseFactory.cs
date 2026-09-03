using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Dtos.AuthModule;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Services
{
    public interface ILoginResponseFactory
    {
        Task<LoginUserResponse> CreateAsync(AppUser user, CancellationToken cancellationToken);
    }

    public class LoginResponseFactory : ILoginResponseFactory
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefreshTokenStore _refreshTokens;

        public LoginResponseFactory(
            UserManager<AppUser> userManager,
            IJwtService jwtService,
            IUnitOfWork unitOfWork,
            IRefreshTokenStore refreshTokens)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _unitOfWork = unitOfWork;
            _refreshTokens = refreshTokens;
        }

        public async Task<LoginUserResponse> CreateAsync(AppUser user, CancellationToken cancellationToken)
        {
            var kurinKey = user.ResolveScopeKurinKeyString();

            var roles = await _userManager.GetRolesAsync(user);
            var jwt = new JwtResponse
            {
                AccessToken = _jwtService.GenerateAccessToken(user.Id.ToString(), user.Email!, roles, kurinKey),
                RefreshToken = _jwtService.GenerateRefreshToken()
            };

            // Adds a session rather than replacing the account's one token: signing in here must not
            // sign the same person out somewhere else.
            await _refreshTokens.IssueAsync(user.Id, jwt.RefreshToken.Token, jwt.RefreshToken.Expires, cancellationToken);

            var member = await _unitOfWork.Members.GetByUserKeyAsync(user.Id, cancellationToken);

            return new LoginUserResponse
            {
                UserKey = user.Id,
                MemberKey = member?.MemberKey,
                Email = user.Email!,
                IsAdmin = roles.Contains(SystemRole.Admin, StringComparer.OrdinalIgnoreCase),
                Permissions = RolePermissionMap.Resolve(roles).Select(permission => permission.ToClaimValue()).ToArray(),
                Roles = roles.ToArray(),
                KurinKey = kurinKey,
                RequiresMfa = false,
                Tokens = jwt
            };
        }
    }
}
