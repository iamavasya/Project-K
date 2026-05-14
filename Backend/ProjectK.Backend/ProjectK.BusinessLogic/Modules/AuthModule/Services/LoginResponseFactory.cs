using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
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

        public LoginResponseFactory(
            UserManager<AppUser> userManager,
            IJwtService jwtService,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _unitOfWork = unitOfWork;
        }

        public async Task<LoginUserResponse> CreateAsync(AppUser user, CancellationToken cancellationToken)
        {
            var kurinKey = user.KurinKey is null || user.KurinKey == Guid.Empty
                ? null
                : user.KurinKey.ToString();

            var roles = await _userManager.GetRolesAsync(user);
            var jwt = new JwtResponse
            {
                AccessToken = _jwtService.GenerateAccessToken(user.Id.ToString(), user.Email!, roles, kurinKey),
                RefreshToken = _jwtService.GenerateRefreshToken()
            };

            user.RefreshToken = jwt.RefreshToken.Token;
            user.RefreshTokenExpiryTime = jwt.RefreshToken.Expires;
            await _userManager.UpdateAsync(user);

            var member = await _unitOfWork.Members.GetByUserKeyAsync(user.Id, cancellationToken);

            return new LoginUserResponse
            {
                UserKey = user.Id,
                MemberKey = member?.MemberKey,
                Email = user.Email!,
                Role = roles.FirstOrDefault()!,
                KurinKey = kurinKey,
                RequiresMfa = false,
                Tokens = jwt
            };
        }
    }
}
