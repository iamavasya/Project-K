import { HttpContext, HttpContextToken } from '@angular/common/http';

/**
 * Що `RequestFeedbackInterceptor` каже людині про запит.
 *
 * - `auto` — тост про успіх мутації, яку запустила людина, і тост про помилку;
 * - `errors` — лише помилки: POST, що нічого не змінює (вхід, перегляд імпорту, експорт), або дія,
 *   після якої сторінка й так міняється;
 * - `silent` — нічого: службові запити, про які людина не просила (оновлення сесії, перевірка
 *   доступу), і ті, де сторінка навмисно мовчить (відновлення пароля не каже, чи є такий акаунт).
 */
export type RequestFeedback = 'auto' | 'errors' | 'silent';

export const REQUEST_FEEDBACK = new HttpContextToken<RequestFeedback>(() => 'auto');

/**
 * Статуси, які сторінка показує сама і по-своєму. 409 на підписі пункту проби значить «уже
 * підписано», а не «не вдалося» — загальний тост тут сказав би неправду.
 */
export const HANDLED_STATUSES = new HttpContextToken<readonly number[]>(() => []);

export function requestFeedback(level: RequestFeedback, handledStatuses: readonly number[] = []): HttpContext {
  return new HttpContext().set(REQUEST_FEEDBACK, level).set(HANDLED_STATUSES, handledStatuses);
}
