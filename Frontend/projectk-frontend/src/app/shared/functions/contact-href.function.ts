/**
 * Посилання, що відкриває дзвінок. Номер у базі записано для людини (`+380 97 313 73 53`), а
 * `tel:` чекає самі цифри з плюсом — пробіли, дужки й дефіси телефон на iOS не завжди прощає.
 */
export function phoneHref(phone: string | null | undefined): string | null {
  const digits = (phone ?? '').replace(/[^\d+]/g, '');
  return digits.replace(/\+/g, '').length >= 5 ? `tel:${digits}` : null;
}

export function emailHref(email: string | null | undefined): string | null {
  const address = (email ?? '').trim();
  return address.includes('@') ? `mailto:${address}` : null;
}
