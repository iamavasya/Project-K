using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.RefreshToken.Refresh
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ServiceResult<JwtResponse>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenStore _refreshTokens;

        public RefreshTokenCommandHandler(
            UserManager<AppUser> userManager,
            IJwtService jwtService,
            IRefreshTokenStore refreshTokens)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _refreshTokens = refreshTokens;
        }

        public async Task<ServiceResult<JwtResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var session = await _refreshTokens.FindActiveAsync(request.RefreshToken, cancellationToken);
            if (session is null)
            {
                return new ServiceResult<JwtResponse>(ResultType.Unauthorized);
            }

            var user = await _userManager.FindByIdAsync(session.UserId.ToString());
            if (user is null)
            {
                return new ServiceResult<JwtResponse>(ResultType.Unauthorized);
            }

            // Spend the token first, and only continue if this call is the one that spent it. Two
            // refreshes racing on the same cookie both find it active a moment earlier; without this
            // both would mint a session, and the one whose response the browser discarded would stay
            // alive, unreachable and beyond the reach of logout.
            if (!await _refreshTokens.RevokeAsync(session.Token, cancellationToken))
            {
                return new ServiceResult<JwtResponse>(ResultType.Unauthorized);
            }

            var jwt = new JwtResponse
            {
                AccessToken = _jwtService.GenerateAccessToken(
                    user.Id.ToString(),
                    user.Email,
                    await _userManager.GetRolesAsync(user),
                    user.ResolveScopeKurinKeyString()),
                RefreshToken = _jwtService.GenerateRefreshToken()
            };

            await _refreshTokens.IssueAsync(user.Id, jwt.RefreshToken.Token, jwt.RefreshToken.Expires, cancellationToken);

            return new ServiceResult<JwtResponse>(ResultType.Success, jwt);
        }
    }
}
