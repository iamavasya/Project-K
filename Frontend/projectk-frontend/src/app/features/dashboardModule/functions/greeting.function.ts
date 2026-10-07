const WEEKDAYS = ['неділя', 'понеділок', 'вівторок', 'середа', 'четвер', 'пʼятниця', 'субота'];
const MONTHS = ['січня', 'лютого', 'березня', 'квітня', 'травня', 'червня', 'липня', 'серпня', 'вересня', 'жовтня', 'листопада', 'грудня'];

/** «Доброго ранку» until noon, «Доброго дня» until five, «Доброго вечора» after; the small hours get «Доброї ночі». */
export function greeting(now: Date): string {
  const hour = now.getHours();
  if (hour < 5) {
    return 'Доброї ночі';
  }
  if (hour < 12) {
    return 'Доброго ранку';
  }
  return hour < 17 ? 'Доброго дня' : 'Доброго вечора';
}

/** «середа, 7 жовтня 2026» — the app carries no Ukrainian locale for DatePipe, so the words are here. */
export function todayLabel(now: Date): string {
  return `${WEEKDAYS[now.getDay()]}, ${now.getDate()} ${MONTHS[now.getMonth()]} ${now.getFullYear()}`;
}

/** The first letters of the first and last name, for an avatar with no photo. */
export function initials(firstName: string | null | undefined, lastName: string | null | undefined): string {
  return `${(firstName ?? '').trim().charAt(0)}${(lastName ?? '').trim().charAt(0)}`.toUpperCase();
}
