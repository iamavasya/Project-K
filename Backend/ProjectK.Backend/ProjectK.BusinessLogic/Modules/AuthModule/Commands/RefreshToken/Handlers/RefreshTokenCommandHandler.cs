using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.RefreshToken.Handlers
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ServiceResult<JwtResponse>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IJwtService _jwtService;
        public RefreshTokenCommandHandler(UserManager<AppUser> userManager, IJwtService jwtService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
        }
        public async Task<ServiceResult<JwtResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.RefreshToken == request.RefreshToken);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return new ServiceResult<JwtResponse>(ResultType.Unauthorized);
            }

            string? kurinKey = user.KurinKey == Guid.Empty ? null : user.KurinKey.ToString();

            var jwt = new JwtResponse
            {
                AccessToken = _jwtService.GenerateAccessToken(user.Id.ToString(), user.Email, await _userManager.GetRolesAsync(user), kurinKey),
                RefreshToken = _jwtService.GenerateRefreshToken()
            };
            user.RefreshToken = jwt.RefreshToken.Token;
            user.RefreshTokenExpiryTime = jwt.RefreshToken.Expires;
            await _userManager.UpdateAsync(user);

            return new ServiceResult<JwtResponse>(
                ResultType.Success,
                new JwtResponse
                {
                    AccessToken = jwt.AccessToken,
                    RefreshToken = jwt.RefreshToken
                }
            );
        }
    }
}
