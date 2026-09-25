import { HttpContext, HttpContextToken } from '@angular/common/http';

export type RequestFeedback = 'auto' | 'errors' | 'silent';

export const REQUEST_FEEDBACK = new HttpContextToken<RequestFeedback>(() => 'auto');

export const HANDLED_STATUSES = new HttpContextToken<readonly number[]>(() => []);

export function requestFeedback(level: RequestFeedback, handledStatuses: readonly number[] = []): HttpContext {
  return new HttpContext().set(REQUEST_FEEDBACK, level).set(HANDLED_STATUSES, handledStatuses);
}
