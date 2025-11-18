export function toDateOnlyString(d: Date | string | null | undefined): string | null {
    if (!d) return null;
    if (typeof d === 'string') {
      if (/^\d{4}-\d{2}-\d{2}$/.test(d)) return d;
      const only = d.split('T')[0];
      return only;
    }
    const y = d.getFullYear();
    const m = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${y}-${m}-${day}`;
  }