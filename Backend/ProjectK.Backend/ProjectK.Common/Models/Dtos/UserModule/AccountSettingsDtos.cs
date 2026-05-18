namespace ProjectK.Common.Models.Dtos.UserModule
{
    public record AccountSettingsDto(
        Guid UserKey,
        Guid? MemberKey,
        string Email,
        string? PhoneNumber,
        string FirstName,
        string LastName,
        string Role,
        bool TwoFactorEnabled,
        string? PendingEmail = null);

    public record UpdateAccountProfileRequestDto(string Email, string? PhoneNumber, string? CurrentPassword = null);

    public record ConfirmAccountEmailChangeRequestDto(string Email, string Token);

    public record ChangePasswordRequestDto(string CurrentPassword, string NewPassword);

    public record ResetMfaRequestDto(string CurrentPassword);

    public record DisableMfaRequestDto(string CurrentPassword);
}
