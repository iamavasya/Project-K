using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos.UserModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Queries.Handlers
{
    public class GetAccountSettingsQueryHandler : IRequestHandler<GetAccountSettingsQuery, ServiceResult<AccountSettingsDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public GetAccountSettingsQueryHandler(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<AccountSettingsDto>> Handle(GetAccountSettingsQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserKey.ToString());
            if (user == null)
            {
                return new ServiceResult<AccountSettingsDto>(ResultType.NotFound);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var member = await _unitOfWork.Members.GetByUserKeyAsync(user.Id, cancellationToken);

            var dto = new AccountSettingsDto(
                user.Id,
                member?.MemberKey,
                user.Email!,
                user.PhoneNumber,
                user.FirstName,
                user.LastName,
                roles.FirstOrDefault()!,
                user.TwoFactorEnabled);

            return new ServiceResult<AccountSettingsDto>(ResultType.Success, dto);
        }
    }
}
