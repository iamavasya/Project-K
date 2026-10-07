import { PlastYearDto, QuarterDto } from '../models/group-dues.dto';
import { quarterKey, quarterLabel } from './dues-format.function';

export interface QuarterOption {
  label: string;
  value: string;
  quarter: QuarterDto;
}

/** Quarters a rate or a пільга may start from: the years on the table plus the one after now. */
export function fromQuarterOptions(years: PlastYearDto[], current: QuarterDto): QuarterOption[] {
  const next: QuarterDto = current.number === 4
    ? { year: current.year + 1, number: 1 }
    : { year: current.year, number: current.number + 1 };
  const seen = new Set<string>();
  return [...years.flatMap(y => y.quarters), next]
    .filter(q => {
      const key = quarterKey(q);
      if (seen.has(key)) {
        return false;
      }
      seen.add(key);
      return true;
    })
    .sort((a, b) => a.year - b.year || a.number - b.number)
    .map(q => ({ label: quarterLabel(q), value: quarterKey(q), quarter: q }));
}
