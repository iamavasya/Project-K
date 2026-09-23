/** The one phone mask every form uses: country code first, so nobody types a leading zero twice. */
export const UKRAINIAN_PHONE_MASK = '+380 99 999 99 99';
export const UKRAINIAN_PHONE_PLACEHOLDER = '+380 __ ___ __ __';

/**
 * Brings a stored number into the shape the mask shows. Numbers saved before the mask was
 * unified look like `0501234567` or `+38 (050) 123-45-67`; either has the same nine digits
 * after the country code, and those are what the mask needs. Anything that is not a Ukrainian
 * mobile-length number is returned as it was, so a foreign number is not mangled.
 */
export function toUkrainianPhone(value: string | null | undefined): string {
  if (!value) {
    return '';
  }

  const digits = value.replace(/\D/g, '');
  const national = nationalPart(digits);
  if (national === null) {
    return value;
  }

  return `+380 ${national.slice(0, 2)} ${national.slice(2, 5)} ${national.slice(5, 7)} ${national.slice(7, 9)}`;
}

/** The nine digits after the country code, or null when the number is not a Ukrainian one. */
function nationalPart(digits: string): string | null {
  if (digits.startsWith('380') && digits.length === 12) {
    return digits.slice(3);
  }
  if (digits.startsWith('0') && digits.length === 10) {
    return digits.slice(1);
  }
  return digits.length === 9 ? digits : null;
}
