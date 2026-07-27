using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.KurinScope.Handlers
{
    public class SetKurinScopeCommandHandler : IRequestHandler<SetKurinScopeCommand, ServiceResult<LoginUserResponse>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoginResponseFactory _loginResponseFactory;

        public SetKurinScopeCommandHandler(
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            ILoginResponseFactory loginResponseFactory)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _loginResponseFactory = loginResponseFactory;
        }

        public async Task<ServiceResult<LoginUserResponse>> Handle(
            SetKurinScopeCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserKey.ToString());
            if (user is null)
            {
                return new ServiceResult<LoginUserResponse>(ResultType.Unauthorized);
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(UserRole.Admin.ToClaimValue(), StringComparer.OrdinalIgnoreCase))
            {
                return ServiceResult<LoginUserResponse>.Failure(
                    ResultType.Forbidden,
                    "kurin_scope_forbidden",
                    "Only an admin can change kurin scope.");
            }

            if (request.KurinKey is not null)
            {
                var kurin = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey.Value, cancellationToken);
                if (kurin is null)
                {
                    return ServiceResult<LoginUserResponse>.Failure(
                        ResultType.NotFound,
                        "kurin_not_found",
                        "Kurin to scope into was not found.");
                }
            }

            user.ActiveKurinKey = request.KurinKey;
            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                return ServiceResult<LoginUserResponse>.Failure(
                    ResultType.BadRequest,
                    "kurin_scope_not_saved",
                    "Kurin scope could not be saved.");
            }

            var response = await _loginResponseFactory.CreateAsync(user, cancellationToken);
            return new ServiceResult<LoginUserResponse>(ResultType.Success, response);
        }
    }
}
