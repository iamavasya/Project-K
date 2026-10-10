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
