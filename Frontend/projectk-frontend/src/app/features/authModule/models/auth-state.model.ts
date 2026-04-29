export interface AuthState {
  userKey: string;
  memberKey: string | null;
  email: string;
  role: string;
  kurinKey: string | null;
  accessToken: string;
}
