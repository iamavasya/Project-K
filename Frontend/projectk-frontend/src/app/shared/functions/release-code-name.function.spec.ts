import { displayCodeName } from './release-code-name.function';

describe('displayCodeName', () => {
  it('returns the code name a release carries', () => {
    expect(displayCodeName('Honeypot Ant')).toBe('Honeypot Ant');
    expect(displayCodeName('  Honeypot Ant  ')).toBe('Honeypot Ant');
  });

  // Since 1.0 a release may be published without one, and the build stamps an empty string here.
  it('reads an absent code name as absent', () => {
    expect(displayCodeName('')).toBeNull();
    expect(displayCodeName('   ')).toBeNull();
    expect(displayCodeName(null)).toBeNull();
    expect(displayCodeName(undefined)).toBeNull();
  });

  it('hides the placeholders an uninjected build carries', () => {
    expect(displayCodeName('LocalDevelopment')).toBeNull();
    expect(displayCodeName('TailscaleDevelopment')).toBeNull();
  });
});
