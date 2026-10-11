/**
 * «Не зараз» on an install hint, remembered on this device for a while. Storage can be missing or
 * refuse (private mode, blocked site data): then the hint simply comes back next time.
 */
const DAY = 86_400_000;

export function dismissedRecently(key: string, days: number, now = Date.now()): boolean {
  try {
    const at = Number(globalThis.localStorage?.getItem(key));
    return Number.isFinite(at) && at > 0 && now - at < days * DAY;
  } catch {
    return false;
  }
}

export function rememberDismissal(key: string, now = Date.now()): void {
  try {
    globalThis.localStorage?.setItem(key, String(now));
  } catch {
    // Not remembered: the hint shows again on the next visit, which is all that is lost.
  }
}
