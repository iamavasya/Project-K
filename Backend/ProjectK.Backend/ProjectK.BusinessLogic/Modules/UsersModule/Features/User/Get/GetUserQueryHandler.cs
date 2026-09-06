using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.UsersModule.Models;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.User.Get
{
    public class GetUserQueryHandler : IRequestHandler<GetUserQuery, ServiceResult<UserDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public GetUserQueryHandler(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<UserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserKey.ToString());
            if (user == null)
            {
                return new ServiceResult<UserDto>(ResultType.NotFound);
            }

            var kurins = (await _unitOfWork.Kurins.GetAllAsync(cancellationToken))
                .ToDictionary(k => k.KurinKey, k => k.Number);
            
            // Only the system role is shown here; offices belong to a kurin and are asked for per kurin.
            var isAdmin = await _userManager.IsInRoleAsync(user, SystemRole.Admin);
            
            var userDto = new UserDto
            {
                UserId = user.Id,
                KurinKey = user.KurinKey,
                KurinNumber = user.KurinKey.HasValue && kurins.TryGetValue(user.KurinKey.Value, out var number) ? number : null,
                Email = user.Email!,
                Role = isAdmin ? SystemRole.Admin : SystemRole.Member,
                TwoFactorEnabled = user.TwoFactorEnabled,
                FirstName = user.FirstName!,
                LastName = user.LastName!
            };

            return new ServiceResult<UserDto>(ResultType.Success, userDto);
        }
    }
}
