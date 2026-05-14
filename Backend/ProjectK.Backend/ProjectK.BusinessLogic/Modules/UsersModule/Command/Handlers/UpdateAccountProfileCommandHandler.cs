using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectK.BusinessLogic.Modules.UsersModule.Queries;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.UserModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Settings;
using System.Net;
using System.Net.Mail;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command.Handlers
{
    public class UpdateAccountProfileCommandHandler : IRequestHandler<UpdateAccountProfileCommand, ServiceResult<AccountSettingsDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<UpdateAccountProfileCommandHandler> _logger;

        public UpdateAccountProfileCommandHandler(
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            IMediator mediator,
            IEmailService emailService,
            IOptions<EmailSettings> emailSettings,
            ILogger<UpdateAccountProfileCommandHandler> logger)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<ServiceResult<AccountSettingsDto>> Handle(UpdateAccountProfileCommand request, CancellationToken cancellationToken)
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

            var currentEmail = user.Email ?? string.Empty;
            var emailChanged = !string.Equals(currentEmail, email, StringComparison.OrdinalIgnoreCase);
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return ServiceResult<AccountSettingsDto>.Failure(ResultType.Conflict, "EmailAlreadyInUse", "Email is already in use.");
            }

            if (emailChanged)
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                {
                    return ServiceResult<AccountSettingsDto>.Failure(ResultType.Unauthorized, "InvalidCredentials", "Current password is required to change email.");
                }

                var passwordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
                if (!passwordValid)
                {
                    return ServiceResult<AccountSettingsDto>.Failure(ResultType.Unauthorized, "InvalidCredentials", "Invalid current password.");
                }
            }

            user.PhoneNumber = request.PhoneNumber?.Trim();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return ServiceResult<AccountSettingsDto>.Failure(ResultType.BadRequest, "UpdateFailed", "Failed to update profile.");
            }

            var member = await _unitOfWork.Members.GetTrackedByUserKeyAsync(user.Id, cancellationToken);
            if (member != null)
            {
                member.PhoneNumber = user.PhoneNumber ?? string.Empty;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var settingsResult = await _mediator.Send(new GetAccountSettingsQuery(user.Id), cancellationToken);
            if (!emailChanged || settingsResult.Data == null)
            {
                return settingsResult;
            }

            var token = await _userManager.GenerateChangeEmailTokenAsync(user, email);
            var confirmationUrl = BuildEmailChangeConfirmationUrl(email, token);
            var body = BuildEmailChangeConfirmationBody(currentEmail, email, confirmationUrl);

            _logger.LogInformation("Audit: User {UserId} requested an email change from {CurrentEmail} to {NewEmail}.", user.Id, currentEmail, email);

            await _emailService.SendEmailAsync(
                email,
                "ProjectK - Confirm email change",
                body,
                cancellationToken);

            return new ServiceResult<AccountSettingsDto>(
                ResultType.Success,
                settingsResult.Data with { PendingEmail = email });
        }

        private string BuildEmailChangeConfirmationUrl(string email, string token)
        {
            var baseUrl = _emailSettings.BaseUrl.TrimEnd('/');
            return $"{baseUrl}/settings/account?confirmEmail=true&email={WebUtility.UrlEncode(email)}&token={WebUtility.UrlEncode(token)}";
        }

        private static string BuildEmailChangeConfirmationBody(string currentEmail, string newEmail, string confirmationUrl)
        {
            return $@"
                <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2>Confirm email change</h2>
                    <p>We received a request to change your ProjectK account email from <strong>{WebUtility.HtmlEncode(currentEmail)}</strong> to <strong>{WebUtility.HtmlEncode(newEmail)}</strong>.</p>
                    <div style='margin: 30px 0;'>
                        <a href='{confirmationUrl}' style='background-color: #007bff; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Confirm email</a>
                    </div>
                    <p>If the button does not work, copy and paste this URL into your browser:</p>
                    <p>{confirmationUrl}</p>
                    <p>If you did not request this change, you can ignore this email.</p>
                </div>";
        }
    }
}
