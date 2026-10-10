import { Component, inject } from '@angular/core';
import {
  IonBackButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonListHeader,
  IonRadio,
  IonRadioGroup,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';
import { AppearanceService, ThemeChoice } from './appearance.service';

const CHOICES: { value: ThemeChoice; label: string }[] = [
  { value: 'system', label: 'Системна' },
  { value: 'light', label: 'Світла' },
  { value: 'dark', label: 'Темна' },
];

/** Системна / Світла / Темна, applied at once and remembered on this device. */
@Component({
  selector: 'app-appearance',
  imports: [
    IonHeader,
    IonToolbar,
    IonButtons,
    IonBackButton,
    IonTitle,
    IonContent,
    IonList,
    IonListHeader,
    IonItemGroup,
    IonItem,
    IonLabel,
    IonRadioGroup,
    IonRadio,
  ],
  styles: `
    .footnote {
      margin: -8px 32px 16px;
      font-size: 13px;
      line-height: 18px;
      color: var(--lk-muted);
    }
    :host-context(.md) .footnote {
      margin: 0 20px 16px;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/more" text="Ще" /></ion-buttons>
        <ion-title>Вигляд</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content [fullscreen]="true">
      <ion-list [inset]="true">
        <ion-list-header><ion-label>Тема</ion-label></ion-list-header>
        <ion-item-group>
          <ion-radio-group [value]="choice()" (ionChange)="pick($event)">
            @for (option of choices; track option.value) {
              <ion-item>
                <ion-radio [value]="option.value" justify="space-between" [attr.data-testid]="'theme-' + option.value">
                  {{ option.label }}
                </ion-radio>
              </ion-item>
            }
          </ion-radio-group>
        </ion-item-group>
      </ion-list>
      <p class="footnote">Системна тема йде за налаштуваннями телефона. Вибір діє лише на цьому пристрої.</p>
    </ion-content>
  `,
})
export class AppearancePage {
  private readonly appearance = inject(AppearanceService);
  protected readonly choices = CHOICES;
  protected readonly choice = this.appearance.choice;

  protected pick(event: Event): void {
    const value = (event as CustomEvent<{ value?: ThemeChoice }>).detail.value;
    if (value) this.appearance.set(value);
  }
}
