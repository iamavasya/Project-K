import { WritableSignal } from '@angular/core';

/** One section of a screen: loading, its value, or a failed read. */
export type Loaded<T> = { state: 'loading' } | { state: 'ready'; value: T } | { state: 'failed' };

export function valueOf<T>(loaded: Loaded<T>): T | null {
  return loaded.state === 'ready' ? loaded.value : null;
}

/** Fills one section; a failed read keeps what was shown before a refresh, if anything. */
export async function settle<T>(request: Promise<T>, target: WritableSignal<Loaded<T>>): Promise<void> {
  try {
    target.set({ state: 'ready', value: await request });
  } catch {
    if (target().state !== 'ready') target.set({ state: 'failed' });
  }
}

/** The line a section shows when its read failed. */
export const FAILED_TEXT = 'Не вдалося завантажити. Потягни вниз, щоб оновити.';
