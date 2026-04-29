using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, ServiceResult<LoginUserResponse>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;

        public LoginUserCommandHandler(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IJwtService jwtService, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<LoginUserResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new ServiceResult<LoginUserResponse>(ResultType.Unauthorized);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (!result.Succeeded)
            {
                return new ServiceResult<LoginUserResponse>(ResultType.Unauthorized);
            }

            string? kurinKey = (user.KurinKey == null || user.KurinKey == Guid.Empty) ? null : user.KurinKey.ToString();

            var role = await _userManager.GetRolesAsync(user);

            var jwt = new JwtResponse
            {
                AccessToken = _jwtService.GenerateAccessToken(user.Id.ToString(), user.Email!, role, kurinKey),
                RefreshToken = _jwtService.GenerateRefreshToken()
            };

            user.RefreshToken = jwt.RefreshToken.Token;
            user.RefreshTokenExpiryTime = jwt.RefreshToken.Expires;
            await _userManager.UpdateAsync(user);

            var member = await _unitOfWork.Members.GetByUserKeyAsync(user.Id, cancellationToken);

            var response = new LoginUserResponse
            {
                UserKey = user.Id,
                MemberKey = member?.MemberKey,
                Email = user.Email!,
                Role = role.FirstOrDefault()!,
                KurinKey = kurinKey,
                Tokens = jwt
            };

            return new ServiceResult<LoginUserResponse>(
                ResultType.Success,
                response);

        }
    }
}
