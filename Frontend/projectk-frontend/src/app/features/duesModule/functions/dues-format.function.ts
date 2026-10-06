import { QuarterDto } from '../models/group-dues.dto';

const hryvnia = new Intl.NumberFormat('uk-UA', { minimumFractionDigits: 0, maximumFractionDigits: 2 });

/** «1 240 ₴», «−300 ₴». A real minus, not a hyphen. */
export function money(amount: number): string {
  const sign = amount < 0 ? '−' : '';
  return `${sign}${hryvnia.format(Math.abs(amount))} ₴`;
}

/** The same with an explicit plus, for movements in and out of the box. */
export function signedMoney(amount: number): string {
  return amount > 0 ? `+${money(amount)}` : money(amount);
}

const ROMAN = ['I', 'II', 'III', 'IV'];

/** «II кв. 2026». */
export function quarterLabel(quarter: QuarterDto): string {
  return `${ROMAN[quarter.number - 1]} кв. ${quarter.year}`;
}

/** «II 2026» — for a column head. */
export function quarterShort(quarter: QuarterDto): string {
  return `${ROMAN[quarter.number - 1]} ${quarter.year}`;
}

export function quarterKey(quarter: QuarterDto): string {
  return `${quarter.year}-${quarter.number}`;
}

export function sameQuarter(a: QuarterDto, b: QuarterDto): boolean {
  return a.year === b.year && a.number === b.number;
}

/** Calendar quarter of a date. */
export function quarterOf(date: Date): QuarterDto {
  return { year: date.getFullYear(), number: Math.floor(date.getMonth() / 3) + 1 };
}
