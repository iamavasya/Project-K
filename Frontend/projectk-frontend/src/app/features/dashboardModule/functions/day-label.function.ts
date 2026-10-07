const WEEKDAYS_SHORT = ['нд', 'пн', 'вт', 'ср', 'чт', 'пт', 'сб'];
const MONTHS_SHORT = ['січ', 'лют', 'бер', 'кві', 'тра', 'чер', 'лип', 'сер', 'вер', 'жов', 'лис', 'гру'];

/** «сьогодні», «завтра», else «сб, 11 жов» — relative to <paramref name="today"/>, by calendar day. */
export function dayLabel(date: Date, today: Date): string {
  const diff = startOfDay(date).getTime() - startOfDay(today).getTime();
  const days = Math.round(diff / 86_400_000);
  if (days === 0) {
    return 'сьогодні';
  }
  if (days === 1) {
    return 'завтра';
  }
  return `${WEEKDAYS_SHORT[date.getDay()]}, ${date.getDate()} ${MONTHS_SHORT[date.getMonth()]}`;
}

/** «10:00» or «10:00–12:00»; nothing for an all-day item. */
export function timeLabel(start: Date, end: Date | null, isAllDay: boolean): string {
  if (isAllDay) {
    return '';
  }
  const from = clock(start);
  return end ? `${from}–${clock(end)}` : from;
}

function clock(date: Date): string {
  return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`;
}

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}
