/**
 * Another account this browser is signed in to, from `auth/accounts`. Kept in step with the API's
 * SavedAccountDto and the PWA's copy in mobile/src/app/auth/auth.models.ts.
 */
export interface SavedAccount {
  userKey: string;
  memberKey: string | null;
  email: string;
  /** Empty for an account without a member, such as a bare administrator. */
  fullName: string;
  profilePhotoUrl: string | null;
  isAdmin: boolean;
  /** Raw system-role names for the kurin the account was last in, for display only. */
  roles: string[];
}
