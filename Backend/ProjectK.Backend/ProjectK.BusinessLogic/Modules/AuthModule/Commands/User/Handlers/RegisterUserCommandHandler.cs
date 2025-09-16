using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, ServiceResult<RegisterUserResponse>>
    {
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly RoleManager<AppRole> _roleManager;

        public RegisterUserCommandHandler(IMapper mapper, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IJwtService jwtService)
        {
            _userManager = userManager;
            _mapper = mapper;
            _jwtService = jwtService;
            _roleManager = roleManager;
        }

        public async Task<ServiceResult<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var user = _mapper.Map<AppUser>(request);
            user.UserName = request.Email;

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ApplicationException($"User registration failed: {errors}");
            }
            
            var roleName = "User";

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new AppRole(roleName));
            }

            await _userManager.AddToRoleAsync(user, roleName);

            var roles = await _userManager.GetRolesAsync(user);

            var jwt = new JwtResponse
            {
                AccessToken = _jwtService.GenerateAccessToken(user.Id.ToString(), user.Email, roles),
                RefreshToken = _jwtService.GenerateRefreshToken()
            };

            user.RefreshToken = jwt.RefreshToken.Token;
            user.RefreshTokenExpiryTime = jwt.RefreshToken.Expires;

            await _userManager.UpdateAsync(user);

            var response = new RegisterUserResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Tokens = jwt
            };
            return new ServiceResult<RegisterUserResponse>(
                Common.Models.Enums.ResultType.Success,
                response);
        }
    }
}
