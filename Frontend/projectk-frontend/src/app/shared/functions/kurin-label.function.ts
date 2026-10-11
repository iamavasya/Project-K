/**
 * How a kurin is named in a line of text: «к. ч. 47», or «підг. курінь» for a підготовчий курінь,
 * which has no number until it is given one in its settings.
 */
export function kurinLabel(kurinNumber: number | null | undefined): string {
  return kurinNumber === null || kurinNumber === undefined ? 'підг. курінь' : `к. ч. ${kurinNumber}`;
}

/** The same at the start of a sentence: «Курінь ч. 47», «Підготовчий курінь». */
export function kurinName(kurinNumber: number | null | undefined): string {
  return kurinNumber === null || kurinNumber === undefined ? 'Підготовчий курінь' : `Курінь ч. ${kurinNumber}`;
}

/** Sorts kurins by number, підготовчі last. */
export function compareKurinNumbers(a: number | null | undefined, b: number | null | undefined): number {
  return (a ?? Number.MAX_SAFE_INTEGER) - (b ?? Number.MAX_SAFE_INTEGER);
}
