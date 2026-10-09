namespace ProjectK.Common.Models.Dtos.AuthModule;

public record MfaSetupResponseDto(string SharedKey, string AuthenticatorUri, string QrCodeBase64);
public record MfaVerifyRequestDto(string Code);
/// <summary>
/// What enabling the second factor answers with. <paramref name="Tokens"/> is the session that
/// replaces the ones just ended: minted after the change, so its <c>amr</c> already says so and the
/// browser carries on without signing in again.
/// </summary>
public record MfaEnableResponseDto(bool Enabled, IEnumerable<string> RecoveryCodes, JwtResponse? Tokens = null);
public record MfaRecoveryCodesRequestDto(string CurrentPassword);
public record MfaRecoveryCodesResponseDto(IEnumerable<string> RecoveryCodes);

/// <summary>
/// The second step of a sign-in. <paramref name="MfaToken"/> is what the password step answered
/// with; without it a code alone is refused.
/// </summary>
public record MfaLoginRequestDto(string Email, string Code, bool RememberMe, string? MfaToken = null);
