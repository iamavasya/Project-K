import { Params } from '@angular/router';
import { ScorePeriodDto, ScorePeriodQuery } from '../models/score.dto';

const decimal = new Intl.NumberFormat('uk-UA', { minimumFractionDigits: 0, maximumFractionDigits: 2 });

/** «12,5» — a гурток's score, which may be an average. A real minus, not a hyphen. */
export function score(value: number): string {
  return value < 0 ? `−${decimal.format(Math.abs(value))}` : decimal.format(value);
}

/** «+3», «−1», «0» — points given or taken. */
export function points(value: number): string {
  if (value > 0) {
    return `+${decimal.format(value)}`;
  }
  return value < 0 ? `−${decimal.format(Math.abs(value))}` : '0';
}

/** One string a select can hold for a period: «year:2026» or «stage:<key>». */
export function periodValue(period: ScorePeriodDto): string {
  return period.kind === 'Year' ? `year:${period.year}` : `stage:${period.stageKey}`;
}

export function periodQueryOf(value: string | null): ScorePeriodQuery {
  if (!value) {
    return {};
  }
  const [kind, rest] = value.split(':');
  return kind === 'stage' ? { stageKey: rest } : { year: Number(rest) };
}

/** The query string the pages carry the chosen period in, so a link between them keeps it. */
export function periodParams(query: ScorePeriodQuery): Params {
  if (query.stageKey) {
    return { stage: query.stageKey };
  }
  return query.year ? { year: query.year } : {};
}

export function periodQueryFromParams(params: Params): ScorePeriodQuery {
  if (params['stage']) {
    return { stageKey: String(params['stage']) };
  }
  const year = Number(params['year']);
  return Number.isFinite(year) && year > 0 ? { year } : {};
}
