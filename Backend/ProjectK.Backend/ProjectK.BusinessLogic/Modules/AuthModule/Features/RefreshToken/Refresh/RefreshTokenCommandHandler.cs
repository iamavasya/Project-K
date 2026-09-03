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

            var jwt = new JwtResponse
            {
                AccessToken = _jwtService.GenerateAccessToken(
                    user.Id.ToString(),
                    user.Email,
                    await _userManager.GetRolesAsync(user),
                    user.ResolveScopeKurinKeyString()),
                RefreshToken = _jwtService.GenerateRefreshToken()
            };

            // Rotation, one session at a time: this token is spent and its replacement takes its
            // place, while the account's other sessions carry on untouched.
            await _refreshTokens.RevokeAsync(session.Token, cancellationToken);
            await _refreshTokens.IssueAsync(user.Id, jwt.RefreshToken.Token, jwt.RefreshToken.Expires, cancellationToken);

            return new ServiceResult<JwtResponse>(ResultType.Success, jwt);
        }
    }
}
