import { Component, inject, signal } from '@angular/core';
import {
  IonButton,
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardSubtitle,
  IonCardTitle,
} from '@ionic/angular';
import { InstallService } from './install.service';

@Component({
  selector: 'app-install-card',
  imports: [IonCard, IonCardHeader, IonCardTitle, IonCardSubtitle, IonCardContent, IonButton],
  template: `
    @if (install.canPrompt()) {
      <ion-card>
        <ion-card-header>
          <ion-card-title>Встановити Лілейку</ion-card-title>
          <ion-card-subtitle>Іконка на екрані, відкривається як застосунок</ion-card-subtitle>
        </ion-card-header>
        <ion-card-content>
          <ion-button expand="block" (click)="install.prompt()">Встановити</ion-button>
        </ion-card-content>
      </ion-card>
    } @else if (install.needsIosHint() && !hidden()) {
      <ion-card>
        <ion-card-header>
          <ion-card-title>Додай Лілейку на екран</ion-card-title>
          <ion-card-subtitle>Так вона працюватиме як застосунок і зможе надсилати сповіщення</ion-card-subtitle>
        </ion-card-header>
        <ion-card-content>
          <p>Натисни «Поділитися» внизу Safari, потім «На початковий екран».</p>
          <ion-button fill="clear" size="small" (click)="hidden.set(true)">Зрозуміло</ion-button>
        </ion-card-content>
      </ion-card>
    }
  `,
})
export class InstallCard {
  protected readonly install = inject(InstallService);
  protected readonly hidden = signal(false);
}
