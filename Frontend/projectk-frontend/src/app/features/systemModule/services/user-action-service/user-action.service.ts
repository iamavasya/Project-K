import { DOCUMENT } from '@angular/common';
import { Injectable, NgZone, inject } from '@angular/core';

/**
 * Одна дія людини: клік чи сабміт і всі запити, які з нього виросли — і ті, що пішли одразу,
 * і ті, що обробник відповіді запустив слідом (зберегти → перечитати, прочитати → змінити).
 */
export class UserAction {
  /** Тост про успіх показано: на решту запитів ланцюжка другого не треба. */
  successShown = false;

  constructor(readonly button: HTMLElement | null) {}
}

const BUSY_CLASS = 'lil-busy';
const BUSY_ICON_CLASS = 'lil-busy-icon';
const BUTTON_SELECTOR = '.p-button';
const CONFIRM_SELECTOR = '.p-confirmdialog, .p-confirmpopup';

/**
 * Памʼятає останній жест людини (клік, Enter, сабміт форми) до кінця поточної задачі циклу
 * подій. Запит, що стартував у цьому вікні, запустила людина: `RequestFeedbackInterceptor` тоді
 * показує тост, а кнопка, на яку натиснули, крутить спінер і не приймає повторних натискань, доки
 * не прийдуть усі відповіді.
 *
 * Один механізм на весь застосунок замість `[loading]` у кожному компоненті: нова кнопка
 * отримує спінер і захист від подвійного натискання без жодного рядка коду. Власний `[loading]`
 * компонента лишається дозволеним — тоді видно лише його спінер.
 */
@Injectable({ providedIn: 'root' })
export class UserActionService {
  private readonly document = inject(DOCUMENT);
  private readonly zone = inject(NgZone);
  private readonly pending = new Map<HTMLElement, number>();

  private armed: UserAction | null = null;
  private disarmTimer: ReturnType<typeof setTimeout> | null = null;
  /** Кнопка, що відкрила діалог підтвердження: крутитись має вона, а не «Так», яке зникає. */
  private lastButtonOutsideConfirm: HTMLElement | null = null;
  private started = false;

  start(): void {
    if (this.started) {
      return;
    }

    this.started = true;
    // Capture на документі спрацьовує раніше за обробники Angular на самих елементах: жест
    // уже записано, коли компонент стартує запит, а повторне натискання гаситься ще до нього.
    this.zone.runOutsideAngular(() => {
      this.document.addEventListener('click', event => this.onClick(event), true);
      this.document.addEventListener('submit', event => this.onSubmit(event), true);
      this.document.addEventListener('keydown', event => this.onKeydown(event), true);
    });
  }

  /**
   * Викликається інтерцептором синхронно, у момент старту запиту. `null` — запит почався не від
   * жесту: завантаження сторінки, таймер, фонове оновлення. `release()` — коли запит скінчився.
   */
  claim(): { action: UserAction; release: () => void } | null {
    const action = this.armed;
    if (!action) {
      return null;
    }

    const button = action.button?.isConnected ? action.button : null;
    if (button) {
      this.markBusy(button);
    }

    let released = false;
    return {
      action,
      release: () => {
        if (!released && button) {
          this.unmarkBusy(button);
        }
        released = true;
      }
    };
  }

  /**
   * Відповідь прийшла, і зараз її обробник може запустити наступний запит тієї самої дії.
   * Інтерцептор кличе це перед тим, як віддати відповідь компоненту.
   */
  resume(action: UserAction): void {
    this.arm(action);
  }

  private onClick(event: Event): void {
    const target = event.target instanceof Element ? event.target : null;
    if (target?.closest(`.${BUSY_CLASS}`)) {
      event.preventDefault();
      event.stopImmediatePropagation();
      return;
    }

    const button = target?.closest<HTMLElement>(BUTTON_SELECTOR) ?? null;
    const insideConfirm = !!button?.closest(CONFIRM_SELECTOR);
    if (button && !insideConfirm) {
      this.lastButtonOutsideConfirm = button;
    }

    const origin = insideConfirm && this.lastButtonOutsideConfirm?.isConnected ? this.lastButtonOutsideConfirm : button;
    this.arm(new UserAction(origin));
  }

  private onSubmit(event: Event): void {
    const form = event.target instanceof HTMLFormElement ? event.target : null;
    if (form?.querySelector(`.${BUSY_CLASS}`)) {
      event.preventDefault();
      event.stopImmediatePropagation();
      return;
    }

    const submitButton = form?.querySelector<HTMLElement>(`button[type="submit"]${BUTTON_SELECTOR}`) ?? null;
    this.arm(new UserAction(submitButton ?? this.armed?.button ?? null));
  }

  private onKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Enter' && event.key !== ' ') {
      return;
    }

    const target = event.target instanceof Element ? event.target : null;
    if (target?.closest(`.${BUSY_CLASS}`)) {
      event.preventDefault();
      event.stopImmediatePropagation();
      return;
    }

    this.arm(new UserAction(target?.closest<HTMLElement>(BUTTON_SELECTOR) ?? null));
  }

  private arm(action: UserAction): void {
    this.armed = action;
    if (this.disarmTimer !== null) {
      clearTimeout(this.disarmTimer);
    }
    // Мікрозадачі (проміси, синхронні ланцюжки RxJS) встигають до таймера, тож запит, який
    // обробник стартує після `await`, теж зараховується жесту.
    this.disarmTimer = setTimeout(() => {
      this.armed = null;
      this.disarmTimer = null;
    });
  }

  private markBusy(button: HTMLElement): void {
    const count = this.pending.get(button) ?? 0;
    this.pending.set(button, count + 1);
    if (count > 0) {
      return;
    }

    button.classList.add(BUSY_CLASS);
    button.setAttribute('aria-busy', 'true');
    const icon = this.document.createElement('span');
    icon.className = `${BUSY_ICON_CLASS} pi pi-spinner pi-spin`;
    icon.setAttribute('aria-hidden', 'true');
    button.prepend(icon);
  }

  private unmarkBusy(button: HTMLElement): void {
    const count = (this.pending.get(button) ?? 0) - 1;
    if (count > 0) {
      this.pending.set(button, count);
      return;
    }

    this.pending.delete(button);
    button.classList.remove(BUSY_CLASS);
    button.removeAttribute('aria-busy');
    button.querySelectorAll(`:scope > .${BUSY_ICON_CLASS}`).forEach(icon => icon.remove());
  }
}
