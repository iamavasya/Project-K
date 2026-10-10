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
import { NavMode, NavModeService } from '../../nav/nav-mode.service';
import { AppearanceService, ThemeChoice } from './appearance.service';

const CHOICES: { value: ThemeChoice; label: string }[] = [
  { value: 'system', label: 'Системна' },
  { value: 'light', label: 'Світла' },
  { value: 'dark', label: 'Темна' },
];

const NAV_CHOICES: { value: NavMode; label: string }[] = [
  { value: 'tabs', label: 'Вкладки й меню' },
  { value: 'web', label: 'Як у вебі' },
];

/** Системна / Світла / Темна and the navigation, applied at once and remembered on this device. */
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
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/more" text="Меню" /></ion-buttons>
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
      <p class="footnote">Системна тема йде за налаштуваннями телефона.</p>

      <ion-list [inset]="true">
        <ion-list-header><ion-label>Навігація</ion-label></ion-list-header>
        <ion-item-group>
          <ion-radio-group [value]="navMode()" (ionChange)="pickNav($event)">
            @for (option of navChoices; track option.value) {
              <ion-item>
                <ion-radio [value]="option.value" justify="space-between" [attr.data-testid]="'nav-' + option.value">
                  {{ option.label }}
                </ion-radio>
              </ion-item>
            }
          </ion-radio-group>
        </ion-item-group>
      </ion-list>
      <p class="footnote">
        «Вкладки й меню»: щоденне внизу екрана, а «Меню» повторює бокове меню вебу. «Як у вебі»: без вкладок, ☰ угорі
        відкриває бокове меню, як у вебі на телефоні. Вибір діє лише на цьому пристрої.
      </p>
    </ion-content>
  `,
})
export class AppearancePage {
  private readonly appearance = inject(AppearanceService);
  protected readonly choices = CHOICES;
  protected readonly choice = this.appearance.choice;
  private readonly nav = inject(NavModeService);
  protected readonly navChoices = NAV_CHOICES;
  protected readonly navMode = this.nav.mode;

  protected pick(event: Event): void {
    const value = (event as CustomEvent<{ value?: ThemeChoice }>).detail.value;
    if (value) this.appearance.set(value);
  }

  protected pickNav(event: Event): void {
    const value = (event as CustomEvent<{ value?: NavMode }>).detail.value;
    if (value) this.nav.set(value);
  }
}
