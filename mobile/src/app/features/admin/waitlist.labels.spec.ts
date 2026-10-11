import { WaitlistEntry, awaitsDecision, canDecide, canResend, waitlistLine, waitlistSections, waitlistStatus } from './waitlist.labels';

const entry = (key: string, status: WaitlistEntry['verificationStatus'], requestedAtUtc: string, extra: Partial<WaitlistEntry> = {}): WaitlistEntry => ({
  waitlistEntryKey: key,
  firstName: 'Ім',
  lastName: key,
  email: `${key}@example.com`,
  isKurinLeaderCandidate: false,
  verificationStatus: status,
  requestedAtUtc,
  ...extra,
});

describe('waitlist labels', () => {
  it('reads the status by name or by the enum’s number', () => {
    expect(waitlistStatus(entry('a', 'Rejected', ''))).toBe('Rejected');
    expect(waitlistStatus(entry('a', 4, ''))).toBe('ApprovedForInvitation');
    expect(waitlistStatus(entry('a', '1', ''))).toBe('NeedsManualVerification');
    expect(waitlistStatus(entry('a', 'Odd', ''))).toBeNull();
  });

  it('offers the web’s actions: decide on a fresh one, resend an approved one', () => {
    expect(canDecide(entry('a', 'Submitted', ''))).toBe(true);
    expect(canDecide(entry('a', 'NeedsManualVerification', ''))).toBe(false);
    expect(canResend(entry('a', 'ApprovedForInvitation', ''))).toBe(true);
    expect(canResend(entry('a', 'Rejected', ''))).toBe(false);
    expect(awaitsDecision(entry('a', 'NeedsManualVerification', ''))).toBe(true);
    expect(awaitsDecision(entry('a', 'Verified', ''))).toBe(false);
  });

  it('puts what waits first, then the approved, then the rest; newest first', () => {
    const sections = waitlistSections([
      entry('old', 'Submitted', '2026-10-01T10:00:00'),
      entry('done', 'Rejected', '2026-10-02T10:00:00'),
      entry('new', 'Submitted', '2026-10-09T10:00:00'),
      entry('ok', 'ApprovedForInvitation', '2026-10-05T10:00:00Z'),
      entry('check', 'NeedsManualVerification', '2026-10-03T10:00:00Z'),
    ]);
    expect(sections.map((s) => [s.key, s.entries.map((e) => e.waitlistEntryKey)])).toEqual([
      ['waiting', ['new', 'check', 'old']],
      ['approved', ['ok']],
      ['done', ['done']],
    ]);
    expect(waitlistSections([])).toEqual([]);
  });

  it('says where they are from and what they claim', () => {
    expect(waitlistLine(entry('a', 'Submitted', '', { stanytsia: 'Львів', regionOrCountry: 'Україна' }))).toBe('Львів, Україна · звичайний учасник');
    expect(waitlistLine(entry('a', 'Submitted', '', { isKurinLeaderCandidate: true, claimedKurinNameOrNumber: '42' }))).toBe(
      'претендує на курінь 42',
    );
  });
});
