export const mfaStatusCheckedKeyPrefix = 'mfa-status-checked';
export const mfaSetupRequiredKeyPrefix = 'mfa-setup-required';

export function getMfaStatusCheckedKey(userKey: string): string {
  return `${mfaStatusCheckedKeyPrefix}:${userKey}`;
}

export function getMfaSetupRequiredKey(userKey: string): string {
  return `${mfaSetupRequiredKeyPrefix}:${userKey}`;
}

export function clearMfaSessionState(): void {
  for (let i = sessionStorage.length - 1; i >= 0; i--) {
    const key = sessionStorage.key(i);
    if (key?.startsWith(`${mfaStatusCheckedKeyPrefix}:`) || key?.startsWith(`${mfaSetupRequiredKeyPrefix}:`)) {
      sessionStorage.removeItem(key);
    }
  }
}
