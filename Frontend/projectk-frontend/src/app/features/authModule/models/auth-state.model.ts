export interface AuthState {
  userKey: string;
  email: string;
  role: string;
  kurinKey?: string | null;
  accessToken: string;
}