using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.UsersModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System.Net.Mail;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.Get;
using ProjectK.Common.Models.Dtos.UsersModule;
using ProjectK.Common.Interfaces.Modules.AuthModule;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.ConfirmEmailChange
{
    public class ConfirmAccountEmailChangeCommandHandler : IRequestHandler<ConfirmAccountEmailChangeCommand, ServiceResult<AccountSettingsDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IRefreshTokenStore _refreshTokens;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly IActivityLogger _activityLogger;

        public ConfirmAccountEmailChangeCommandHandler(
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            IMediator mediator,
            IActivityLogger activityLogger,
            IRefreshTokenStore refreshTokens)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _activityLogger = activityLogger;
            _refreshTokens = refreshTokens;
        }

        public async Task<ServiceResult<AccountSettingsDto>> Handle(ConfirmAccountEmailChangeCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserKey.ToString());
            if (user == null)
            {
                return ServiceResult<AccountSettingsDto>.Failure(ResultType.Unauthorized, "Unauthorized", "User not found or unauthorized.");
            }

            var email = request.Email.Trim();
            if (!MailAddress.TryCreate(email, out _))
            {
                return ServiceResult<AccountSettingsDto>.Failure(ResultType.BadRequest, "InvalidEmail", "Invalid email format.");
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return ServiceResult<AccountSettingsDto>.Failure(ResultType.Conflict, "EmailAlreadyInUse", "Email is already in use.");
            }

            var changeEmailResult = await _userManager.ChangeEmailAsync(user, email, request.Token);
            if (!changeEmailResult.Succeeded)
            {
                return ServiceResult<AccountSettingsDto>.Failure(ResultType.BadRequest, "InvalidToken", "Invalid or expired email confirmation token.");
            }

            user.UserName = email;
            user.NormalizedUserName = _userManager.NormalizeName(email);
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return ServiceResult<AccountSettingsDto>.Failure(ResultType.BadRequest, "UpdateFailed", "Failed to update profile.");
            }

            // Only once the change is stored. RevokeAllAsync commits on its own, so ending the
            // sessions first signed every device out even when this update failed.
            await RefreshTokenInvalidation.RevokeRefreshTokenAsync(_refreshTokens, user, cancellationToken);

            _activityLogger.LogAudit(
                action: "Account.EmailChangeConfirmed",
                actorUserId: user.Id,
                newEmail: email,
                reason: "Email change confirmed.");

            var member = await _unitOfWork.Members.GetTrackedByUserKeyAsync(user.Id, cancellationToken);
            if (member != null)
            {
                member.Email = email;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return await _mediator.Send(new GetAccountSettingsQuery(user.Id), cancellationToken);
        }
    }
}
