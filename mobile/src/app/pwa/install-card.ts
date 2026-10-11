import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonButton, IonCard, IonCardContent, IonCardHeader, IonCardTitle } from '@ionic/angular';
import { dismissedRecently, rememberDismissal } from './dismissal';
import { InstallService } from './install.service';

/** «Не зараз» keeps the card off Головна on this device for two weeks. */
const DISMISSED_KEY = 'lk-install-card-dismissed';
const DISMISSED_DAYS = 14;

@Component({
  selector: 'app-install-card',
  imports: [IonCard, IonCardHeader, IonCardTitle, IonCardContent, IonButton, RouterLink],
  styles: `
    .buttons {
      display: flex;
      flex-wrap: wrap;
      gap: 4px 8px;
      margin-top: 8px;
    }
  `,
  template: `
    @if (!hidden()) {
      @if (install.canPrompt()) {
        <ion-card data-testid="install-card">
          <ion-card-header>
            <ion-card-title>Встановити Лілейку</ion-card-title>
          </ion-card-header>
          <ion-card-content>
            <p>Іконка на екрані, відкривається як застосунок.</p>
            <div class="buttons">
              <ion-button (click)="install.prompt()">Встановити</ion-button>
              <ion-button fill="clear" (click)="dismiss()">Не зараз</ion-button>
            </div>
          </ion-card-content>
        </ion-card>
      } @else if (install.needsIosHint()) {
        <ion-card data-testid="install-card">
          <ion-card-header>
            <ion-card-title>Додай Лілейку на екран</ion-card-title>
          </ion-card-header>
          <ion-card-content>
            <p>Так вона працюватиме як застосунок і зможе надсилати сповіщення.</p>
            <p>Натисни «Поділитися» в Safari, потім «На початковий екран».</p>
            <div class="buttons">
              <ion-button size="small" routerLink="/install">Показати як</ion-button>
              <ion-button fill="clear" size="small" (click)="dismiss()">Не зараз</ion-button>
            </div>
          </ion-card-content>
        </ion-card>
      }
    }
  `,
})
export class InstallCard {
  protected readonly install = inject(InstallService);
  protected readonly hidden = signal(dismissedRecently(DISMISSED_KEY, DISMISSED_DAYS));

  protected dismiss(): void {
    rememberDismissal(DISMISSED_KEY);
    this.hidden.set(true);
  }
}
