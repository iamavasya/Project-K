import { HttpErrorResponse } from '@angular/common/http';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonButton, IonIcon } from '@ionic/angular';
import { addIcons } from 'ionicons';
import { lockClosed } from 'ionicons/icons';

/** The API refused a read for lack of rights (403), as opposed to failing. */
export function isForbidden(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 403;
}

/**
 * The web's «Немає доступу» page as an empty state a screen shows in place of its content when the
 * API answers 403 (see `isForbidden`). A failed save stays a toast (`apiErrorText` already words a
 * 403 as «Недостатньо прав для цієї дії.»).
 *
 *   @if (forbidden()) { <app-forbidden /> } @else { … }
 */
@Component({
  selector: 'app-forbidden',
  imports: [IonButton, IonIcon, RouterLink],
  styles: `
    :host {
      display: block;
      text-align: center;
      padding: 48px 24px;
    }
    ion-icon {
      font-size: 48px;
      color: var(--lk-faint);
    }
    h2 {
      margin: 12px 0 8px;
      font-size: 20px;
      font-weight: 700;
      color: var(--lk-ink);
    }
    p {
      margin: 0 0 16px;
      color: var(--lk-muted);
    }
  `,
  template: `
    <ion-icon name="lock-closed" aria-hidden="true" />
    <h2>{{ title() }}</h2>
    <p>{{ message() }}</p>
    <ion-button fill="outline" size="default" routerLink="/tabs/home">На головну</ion-button>
  `,
})
export class ForbiddenState {
  readonly title = input('Немає доступу');
  readonly message = input('У тебе немає прав на перегляд цієї сторінки.');

  constructor() {
    addIcons({ lockClosed });
  }
}
