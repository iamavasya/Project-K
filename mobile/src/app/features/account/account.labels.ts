import { AuthState, KurinBranch, KurinScopeOption, MembershipKind } from '../../auth/auth.models';
import { MyGroupDto } from './account.models';

/** The web's KURIN_BRANCH_LABELS and MEMBERSHIP_KIND_LABELS. */
export const BRANCH_LABELS: Record<KurinBranch, string> = { UPYu: 'УПЮ', USP: 'УСП', UPS: 'УПС' };
export const KIND_LABELS: Record<MembershipKind, string> = { Youth: 'Юнацтво', Staff: 'Виховний склад' };

/** «к. ч. 7» */
export function kurinShortLabel(option: Pick<KurinScopeOption, 'kurinNumber'>): string {
  return `к. ч. ${option.kurinNumber}`;
}

/** «к. ч. 7 ім. Сірого Лева», as the web's my-kurins tile names a kurin. */
export function kurinLabel(option: Pick<KurinScopeOption, 'kurinNumber' | 'namedAfter'>): string {
  return option.namedAfter ? `к. ч. ${option.kurinNumber} ім. ${option.namedAfter}` : kurinShortLabel(option);
}

/** The one acted in first, then by number (the web tile's order). */
export function sortKurins(options: KurinScopeOption[], currentKey: string | null): KurinScopeOption[] {
  return [...options].sort(
    (a, b) =>
      Number(b.kurinKey === currentKey) - Number(a.kurinKey === currentKey) || a.kurinNumber - b.kurinNumber,
  );
}

/** Leading one's own гурток is sitting in its провід; leading another's is being its впорядник. */
export function groupRole(group: Pick<MyGroupDto, 'isOwn' | 'isLed'>): string {
  if (group.isOwn) return group.isLed ? 'мій гурток · провід' : 'мій гурток';
  return 'впорядник';
}

/**
 * Admins and whole-kurin managers (Звʼязковий) must keep the second factor: they «Скинути» it,
 * everyone else may «Вимкнути». The web's account-settings decides the same way.
 */
export function isPrivileged(user: Pick<AuthState, 'isAdmin' | 'permissions'> | null): boolean {
  if (!user) return false;
  return user.isAdmin || user.permissions.some((permission) => permission.startsWith('Group:Manage:KurinWide'));
}

export interface PasswordRule {
  readonly id: string;
  readonly label: string;
  readonly test: (password: string) => boolean;
}

/** The web's PASSWORD_RULES (shared/functions/password-rules.function.ts). */
export const PASSWORD_RULES: readonly PasswordRule[] = [
  { id: 'length', label: 'щонайменше 8 символів', test: (password) => password.length >= 8 },
  { id: 'upper', label: 'велика латинська літера (A–Z)', test: (password) => /[A-Z]/.test(password) },
  { id: 'digit', label: 'цифра', test: (password) => /\d/.test(password) },
  { id: 'symbol', label: 'символ, як-от ! ? # або _', test: (password) => /[^A-Za-z0-9]/.test(password) },
];

export function passwordMeetsRules(password: string): boolean {
  return PASSWORD_RULES.every((rule) => rule.test(password));
}

/**
 * The code name to show beside a version, or null: none, or a local build's placeholder
 * (`LocalDevelopment`). The web's release-code-name.function.ts.
 */
export function displayCodeName(codeName: string | null | undefined): string | null {
  const value = codeName?.trim();
  return !value || /development/i.test(value) ? null : value;
}

/** «v1.2.0 «Ant»» or just the version. */
export function versionLabel(version: string, codeName: string | null | undefined): string {
  const code = displayCodeName(codeName);
  return code ? `${version} «${code}»` : version;
}

/** `/health` sits beside `/api`, not under it. */
export function healthUrl(api: string): string {
  const trimmed = api.endsWith('/') ? api.slice(0, -1) : api;
  return trimmed.endsWith('/api') ? `${trimmed.slice(0, -4)}/health` : `${trimmed}/health`;
}

/** The size a picture is drawn at so its longer side is at most `max`; never enlarged. */
export function fitWithin(width: number, height: number, max: number): { width: number; height: number } {
  const longer = Math.max(width, height);
  if (longer <= max || longer === 0) return { width, height };
  const scale = max / longer;
  return { width: Math.round(width * scale), height: Math.round(height * scale) };
}
