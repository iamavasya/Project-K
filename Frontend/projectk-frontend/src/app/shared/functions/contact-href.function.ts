export function phoneHref(phone: string | null | undefined): string | null {
  const digits = (phone ?? '').replaceAll(/[^\d+]/g, '');
  return digits.replaceAll('+', '').length >= 5 ? `tel:${digits}` : null;
}

export function emailHref(email: string | null | undefined): string | null {
  const address = (email ?? '').trim();
  return address.includes('@') ? `mailto:${address}` : null;
}
