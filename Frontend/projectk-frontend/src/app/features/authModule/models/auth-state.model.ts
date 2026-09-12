export interface AuthState {
  userKey: string;
  memberKey: string | null;
  email: string;
  isAdmin: boolean;
  // Backend permission strings, e.g. "Group:Manage:KurinWide" — the single source for UI gating.
  permissions: string[];
  // Raw system-role names (e.g. "KV.Zvyazkovyi"), kept for display only.
  roles: string[];
  kurinKey: string | null;
  accessToken: string | null;
}
