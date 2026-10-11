/**
 * How a kurin is named, as the web's shared/functions/kurin-label.function.ts: a підготовчий курінь
 * has no number until it is given one in its settings.
 */

/** «к. ч. 47», or «підг. курінь». */
export function kurinLabel(kurinNumber: number | null | undefined): string {
  return kurinNumber === null || kurinNumber === undefined ? 'підг. курінь' : `к. ч. ${kurinNumber}`;
}

/** The same at the start of a sentence: «Курінь ч. 47», «Підготовчий курінь». */
export function kurinName(kurinNumber: number | null | undefined): string {
  return kurinNumber === null || kurinNumber === undefined ? 'Підготовчий курінь' : `Курінь ч. ${kurinNumber}`;
}

/** A heading: «47 курінь», «Підготовчий курінь». */
export function kurinTitle(kurinNumber: number | null | undefined): string {
  return kurinNumber === null || kurinNumber === undefined ? 'Підготовчий курінь' : `${kurinNumber} курінь`;
}

/** «у курені ч. 47», «у підготовчому курені». */
export function inKurin(kurinNumber: number | null | undefined): string {
  return kurinNumber === null || kurinNumber === undefined ? 'підготовчому курені' : `курені ч. ${kurinNumber}`;
}

/** By number, підготовчі last. */
export function compareKurinNumbers(a: number | null | undefined, b: number | null | undefined): number {
  return (a ?? Number.MAX_SAFE_INTEGER) - (b ?? Number.MAX_SAFE_INTEGER);
}
