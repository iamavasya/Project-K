export function phoneHref(phone: string | null | undefined): string | null {
  const digits = (phone ?? '').replace(/[^\d+]/g, '');
  return digits.replace(/\+/g, '').length >= 5 ? `tel:${digits}` : null;
}

export function emailHref(email: string | null | undefined): string | null {
  const address = (email ?? '').trim();
  return address.includes('@') ? `mailto:${address}` : null;
}
