import { DOCUMENT } from '@angular/common';
import { Injectable, NgZone, inject } from '@angular/core';

export class UserAction {
  successShown = false;

  constructor(readonly button: HTMLElement | null) {}
}

const BUSY_CLASS = 'lil-busy';
const BUSY_ICON_CLASS = 'lil-busy-icon';
const BUTTON_SELECTOR = '.p-button';
const CONFIRM_SELECTOR = '.p-confirmdialog, .p-confirmpopup';

@Injectable({ providedIn: 'root' })
export class UserActionService {
  private readonly document = inject(DOCUMENT);
  private readonly zone = inject(NgZone);
  private readonly pending = new Map<HTMLElement, number>();

  private armed: UserAction | null = null;
  private disarmTimer: ReturnType<typeof setTimeout> | null = null;
  private lastButtonOutsideConfirm: HTMLElement | null = null;
  private started = false;

  start(): void {
    if (this.started) {
      return;
    }

    this.started = true;
    this.zone.runOutsideAngular(() => {
      this.document.addEventListener('click', event => this.onClick(event), true);
      this.document.addEventListener('submit', event => this.onSubmit(event), true);
      this.document.addEventListener('keydown', event => this.onKeydown(event), true);
    });
  }

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
