import { DuesEntryKind, INCOMING_DUES_KINDS } from '../models/dues.enums';
import { DuesEntryDto, QuarterDto } from '../models/group-dues.dto';

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

/**
 * What an operation does to the box, signed: money in is plus, money out is minus, a correction is
 * as entered. An exchange moves money between cash and card and is neither — null.
 */
export function signedDuesAmount(entry: Pick<DuesEntryDto, 'kind' | 'amount'>): number | null {
  if (entry.kind === DuesEntryKind.Exchange) {
    return null;
  }
  if (entry.kind === DuesEntryKind.Correction) {
    return entry.amount;
  }
  return INCOMING_DUES_KINDS.has(entry.kind) ? entry.amount : -entry.amount;
}

/** The amount as the history shows it: signed, or bare for an exchange. */
export function duesAmountLabel(entry: Pick<DuesEntryDto, 'kind' | 'amount'>): string {
  const signed = signedDuesAmount(entry);
  return signed === null ? money(entry.amount) : signedMoney(signed);
}

/** Whether the history paints the amount as coming in; null for an exchange. */
export function isIncomingDues(entry: Pick<DuesEntryDto, 'kind' | 'amount'>): boolean | null {
  const signed = signedDuesAmount(entry);
  return signed === null ? null : signed > 0;
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
