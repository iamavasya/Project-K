const WEEKDAYS = ['неділя', 'понеділок', 'вівторок', 'середа', 'четвер', 'пʼятниця', 'субота'];
const WEEKDAYS_SHORT = ['нд', 'пн', 'вт', 'ср', 'чт', 'пт', 'сб'];
const MONTHS = ['січня', 'лютого', 'березня', 'квітня', 'травня', 'червня', 'липня', 'серпня', 'вересня', 'жовтня', 'листопада', 'грудня'];
const MONTHS_SHORT = ['січ', 'лют', 'бер', 'кві', 'тра', 'чер', 'лип', 'сер', 'вер', 'жов', 'лис', 'гру'];

/** The web dashboard's wording (dashboardModule/functions), so both greet the same way. */
export function greeting(now: Date): string {
  const hour = now.getHours();
  if (hour < 5) return 'Доброї ночі';
  if (hour < 12) return 'Доброго ранку';
  return hour < 17 ? 'Доброго дня' : 'Доброго вечора';
}

/** «середа, 7 жовтня» */
export function todayLabel(now: Date): string {
  return `${WEEKDAYS[now.getDay()]}, ${now.getDate()} ${MONTHS[now.getMonth()]}`;
}

/** «сьогодні», «завтра», else «сб, 11 жов». */
export function dayLabel(date: Date, today: Date): string {
  const days = Math.round((startOfDay(date) - startOfDay(today)) / 86_400_000);
  if (days === 0) return 'сьогодні';
  if (days === 1) return 'завтра';
  return `${WEEKDAYS_SHORT[date.getDay()]}, ${date.getDate()} ${MONTHS_SHORT[date.getMonth()]}`;
}

/** «10:00» or «10:00–12:00»; nothing for an all-day item. */
export function timeLabel(start: Date, end: Date | null, isAllDay: boolean): string {
  if (isAllDay) return '';
  return end ? `${clock(start)}–${clock(end)}` : clock(start);
}

/** «7 жовтня 2010» */
export function dateLabel(date: Date): string {
  return `${date.getDate()} ${MONTHS[date.getMonth()]} ${date.getFullYear()}`;
}

export function initials(firstName: string | null | undefined, lastName: string | null | undefined): string {
  return `${(firstName ?? '').trim().charAt(0)}${(lastName ?? '').trim().charAt(0)}`.toUpperCase();
}

/** «150 ₴» with the sign kept for a debt. */
export function money(amount: number): string {
  return `${new Intl.NumberFormat('uk-UA', { maximumFractionDigits: 2 }).format(amount)} ₴`;
}

function clock(date: Date): string {
  return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`;
}

function startOfDay(date: Date): number {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();
}
