using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.RefreshToken.Refresh;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ServiceResult<JwtResponse>>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly IAccessContextResolver _access;

    public RefreshTokenCommandHandler(
        UserManager<AppUser> userManager,
        IJwtService jwtService,
        IRefreshTokenStore refreshTokens,
        IAccessContextResolver access)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _refreshTokens = refreshTokens;
        _access = access;
    }

    public async Task<ServiceResult<JwtResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var session = await _refreshTokens.FindActiveAsync(request.RefreshToken, cancellationToken);
        if (session is null)
        {
            return new ServiceResult<JwtResponse>(ResultType.Unauthorized);
        }

        // Suspension ends every session when it is applied; this is the backstop for a token
        // issued in the gap, so a suspended account cannot keep a session alive by refreshing.
        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null || !user.CanSignIn())
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

        // A refresh is where a change of office reaches the token, so the roles are worked out
        // again rather than carried over — and worked out for the kurin the account is in now.
        var access = await _access.ResolveAsync(user, cancellationToken);

        var jwt = new JwtResponse
        {
            AccessToken = _jwtService.GenerateAccessToken(
                user.Id.ToString(),
                user.Email,
                access.Roles,
                access.KurinKey?.ToString()),
            RefreshToken = _jwtService.GenerateRefreshToken()
        };

        await _refreshTokens.IssueAsync(user.Id, jwt.RefreshToken.Token, jwt.RefreshToken.Expires, cancellationToken);

        return new ServiceResult<JwtResponse>(ResultType.Success, jwt);
    }
}
