import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MessageService, ToastMessageOptions } from '@openng/optimus-ui/api';
import { Observable, finalize, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { UserActionService } from './user-action-service/user-action.service';
import { failureDetail } from '../../../shared/functions/failure-detail.function';
import { HANDLED_STATUSES, REQUEST_FEEDBACK } from '../../../shared/functions/request-feedback.function';

const DUPLICATE_WINDOW_MS = 3000;
const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS']);

@Injectable()
export class RequestFeedbackInterceptor implements HttpInterceptor {
  private readonly messages = inject(MessageService);
  private readonly userActions = inject(UserActionService);
  private toastsShown = 0;
  private lastToast: { key: string; at: number } | null = null;

  constructor() {
    this.messages.messageObserver.subscribe(() => this.toastsShown++);
  }

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const feedback = req.context.get(REQUEST_FEEDBACK);
    if (feedback === 'silent') {
      return next.handle(req);
    }

    const claim = this.userActions.claim();
    const action = claim?.action ?? null;
    const isMutation = !SAFE_METHODS.has(req.method);
    const handled = req.context.get(HANDLED_STATUSES);

    return next.handle(req).pipe(
      tap({
        next: event => {
          if (!(event instanceof HttpResponse) || !action) {
            return;
          }
          if (isMutation && feedback === 'auto' && !action.successShown && !environment.isStaticDemo) {
            action.successShown = true;
            this.showUnlessComponentDid({ severity: 'success', summary: req.method === 'DELETE' ? 'Видалено' : 'Збережено' });
          }
          this.userActions.resume(action);
        },
        error: (error: unknown) => {
          if (action) {
            this.userActions.resume(action);
          }
          if (!isMutation && !action) {
            return;
          }
          if (error instanceof HttpErrorResponse && handled.includes(error.status)) {
            return;
          }
          const toast = errorToast(error, isMutation, req.body instanceof FormData);
          if (toast) {
            this.showUnlessComponentDid(toast);
          }
        }
      }),
      finalize(() => claim?.release())
    );
  }

  private showUnlessComponentDid(toast: ToastMessageOptions): void {
    const shownBefore = this.toastsShown;
    setTimeout(() => {
      if (this.toastsShown !== shownBefore) {
        return;
      }

      const key = `${toast.severity}|${toast.summary}|${toast.detail ?? ''}`;
      const now = Date.now();
      if (this.lastToast?.key === key && now - this.lastToast.at < DUPLICATE_WINDOW_MS) {
        return;
      }

      this.lastToast = { key, at: now };
      this.messages.add(toast);
    });
  }
}

function errorToast(error: unknown, isMutation: boolean, carriesFile: boolean): ToastMessageOptions | null {
  if (!(error instanceof HttpErrorResponse)) {
    return null;
  }

  const failed = isMutation ? 'Не вдалося зберегти' : 'Не вдалося завантажити';
  switch (true) {
    case error.status === 401:
      return null;
    case error.status === 0 && !navigator.onLine:
      return { severity: 'error', summary: 'Звʼязку немає', detail: 'Перевір інтернет і спробуй ще раз.' };
    case error.status === 0 && carriesFile:
      return { severity: 'warn', summary: 'Файл не надіслано', detail: 'Сервер не прийняв файл — найімовірніше, він завеликий. Обери менший.' };
    case error.status === 0:
      return { severity: 'error', summary: 'Сервер не відповів', detail: 'Спробуй ще раз за хвилину.' };
    case error.status === 403:
      return { severity: 'error', summary: 'Немає доступу', detail: failureDetail(error) };
    case error.status === 404:
      return { severity: 'warn', summary: 'Не знайдено', detail: 'Запису вже немає. Онови сторінку.' };
    case error.status === 409:
      return { severity: 'warn', summary: failed, detail: 'Запис уже змінили або такий уже є. Онови сторінку й спробуй ще раз.' };
    case error.status === 413:
      return { severity: 'warn', summary: 'Файл завеликий', detail: 'Обери менший файл.' };
    case error.status === 429:
      return { severity: 'warn', summary: 'Забагато спроб', detail: 'Зачекай хвилину й спробуй знову.' };
    case error.status >= 500:
      return { severity: 'error', summary: 'Щось пішло не так', detail: 'Помилка на сервері. Спробуй за хвилину.' };
    case error.status >= 400:
      return { severity: 'warn', summary: failed, detail: 'Перевір поля: щось заповнено неправильно або пропущено.' };
    default:
      return null;
  }
}
