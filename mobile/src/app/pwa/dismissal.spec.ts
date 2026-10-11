import { dismissedRecently, rememberDismissal } from './dismissal';

describe('install hint dismissal', () => {
  const key = 'lk-test-dismissal';
  afterEach(() => localStorage.removeItem(key));

  it('stays hidden for the given days, then comes back', () => {
    const now = Date.UTC(2026, 9, 11);
    expect(dismissedRecently(key, 14, now)).toBe(false);
    rememberDismissal(key, now);
    expect(dismissedRecently(key, 14, now + 13 * 86_400_000)).toBe(true);
    expect(dismissedRecently(key, 14, now + 15 * 86_400_000)).toBe(false);
  });

  it('ignores what it cannot read', () => {
    localStorage.setItem(key, 'garbage');
    expect(dismissedRecently(key, 14)).toBe(false);
  });
});
