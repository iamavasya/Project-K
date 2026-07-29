import { isUsableKey } from './isUsableKey.function';

describe('isUsableKey', () => {
  it('should accept a real key', () => {
    expect(isUsableKey('3f2b1c4d-0000-4a1b-9c2d-5e6f7a8b9c0d')).toBeTrue();
  });

  it('should reject an empty guid in any case', () => {
    expect(isUsableKey('00000000-0000-0000-0000-000000000000')).toBeFalse();
    expect(isUsableKey('00000000-0000-0000-0000-000000000000'.toUpperCase())).toBeFalse();
  });

  it('should reject blank and missing values', () => {
    expect(isUsableKey('')).toBeFalse();
    expect(isUsableKey('   ')).toBeFalse();
    expect(isUsableKey(null)).toBeFalse();
    expect(isUsableKey(undefined)).toBeFalse();
  });
});
