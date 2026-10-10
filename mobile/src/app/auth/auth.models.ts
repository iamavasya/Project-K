/** The web's shapes (Frontend authModule/models), kept in step so both read the same session. */
export interface AuthState {
  userKey: string;
  memberKey: string | null;
  email: string;
  isAdmin: boolean;
  permissions: string[];
  roles: string[];
  kurinKey: string | null;
  accessToken: string | null;
}

export interface LoginResponse {
  userKey: string;
  memberKey: string | null;
  email: string;
  isAdmin: boolean;
  permissions: string[] | null;
  roles: string[] | null;
  kurinKey: string | null;
  requiresMfa: boolean;
  mfaToken?: string | null;
  tokens: { accessToken: string } | null;
}

export type LoginOutcome = { kind: 'signed-in' } | { kind: 'mfa'; mfaToken: string | null };

export interface MfaStatus {
  isMfaEnabled: boolean;
  /** The account's offices require a second factor (admin, провід куреня). */
  isMfaRequired: boolean;
}

export interface MfaSetup {
  sharedKey: string;
  /** otpauth:// link; on a phone it opens the authenticator app directly. */
  authenticatorUri: string;
  qrCodeBase64: string;
}

export interface MfaEnabled {
  enabled: boolean;
  recoveryCodes: string[];
  /** The session that replaces the ended ones; its token already says the account has a second factor. */
  tokens?: { accessToken: string } | null;
}
