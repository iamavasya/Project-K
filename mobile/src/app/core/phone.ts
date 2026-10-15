import { CountryCode, getCountries, isPossiblePhoneNumber, parsePhoneNumberFromString } from 'libphonenumber-js';

/** The country a number without «+» is read in when the край does not name one. */
export const DEFAULT_PHONE_COUNTRY: CountryCode = 'UA';

const COUNTRIES = new Set<string>(getCountries());

/** The country a край's numbers default to: its code when that is a country (ua, cz, pl…), else Ukraine. */
export function phoneCountryOfKrai(kraiCode: string | null | undefined): CountryCode {
  const code = (kraiCode ?? '').toUpperCase();
  return COUNTRIES.has(code) ? (code as CountryCode) : DEFAULT_PHONE_COUNTRY;
}

/**
 * A typed number as it is stored: «+», the calling code and the digits, as +420777123456. A number
 * typed without «+» is read in the край's country (777 123 456 in Czechia). Empty stays empty;
 * one that cannot be read goes as typed, for the server to refuse in its own words.
 */
export function toStoredPhone(text: string | null | undefined, country: CountryCode): string {
  const typed = (text ?? '').trim();
  if (!typed) return '';
  return parsePhoneNumberFromString(typed, country)?.number ?? typed;
}

/** Whether a typed number could be a real one; empty counts as fine, a required check is separate. */
export function isPossiblePhone(text: string | null | undefined, country: CountryCode): boolean {
  const stored = toStoredPhone(text, country);
  return !stored || isPossiblePhoneNumber(stored);
}

/** A stored number for reading: +420 777 123 456. Anything the library cannot read is shown as it is. */
export function formatPhone(value: string | null | undefined): string {
  const text = (value ?? '').trim();
  if (!text) return '';
  return parsePhoneNumberFromString(text)?.formatInternational() ?? text;
}
