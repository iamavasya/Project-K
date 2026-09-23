/**
 * Часовий пояс цього браузера як IANA-назва — «Europe/Kyiv».
 *
 * Потрібен там, де документ збирає сервер, а читає людина: сервер стоїть у UTC й поясу читача не
 * знає, тож питати треба тут і везти з запитом.
 *
 * Порожній рядок, якщо браузер не назвався: на тому боці це означає UTC, і хай краще буде UTC, ніж
 * впаде звіт.
 */
export function browserTimeZone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone ?? '';
  } catch {
    return '';
  }
}
