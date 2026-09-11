import { toUkrainianPhone } from './ukrainian-phone.function';

describe('toUkrainianPhone', () => {
  it('lays a legacy national number out under the unified mask', () => {
    expect(toUkrainianPhone('0501234567')).toBe('+380 50 123 45 67');
  });

  it('accepts the older masked shape and the bare international one', () => {
    expect(toUkrainianPhone('+38 (050) 123-45-67')).toBe('+380 50 123 45 67');
    expect(toUkrainianPhone('380501234567')).toBe('+380 50 123 45 67');
  });

  it('leaves a number of another length alone rather than guessing', () => {
    expect(toUkrainianPhone('+1 415 555 0100')).toBe('+1 415 555 0100');
    expect(toUkrainianPhone('')).toBe('');
    expect(toUkrainianPhone(null)).toBe('');
  });
});
