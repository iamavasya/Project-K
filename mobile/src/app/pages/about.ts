import { Component, computed, inject, signal } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { Haptics, ImpactStyle } from '@capacitor/haptics';
import {
  IonBackButton,
  IonButton,
  IonButtons,
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardTitle,
  IonContent,
  IonHeader,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonNote,
  IonTitle,
  IonToolbar,
  getPlatforms,
} from '@ionic/angular';
import { environment } from '../../environments/environment';
import { InstallService } from '../pwa/install.service';
import { apiUrl } from '../runtime-config';

/** What the app runs as: the shell checks from Phase 0, kept for support and for the smoke tests. */
@Component({
  selector: 'app-about',
  imports: [
    IonHeader,
    IonToolbar,
    IonButtons,
    IonBackButton,
    IonTitle,
    IonContent,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonButton,
    IonList,
    IonItem,
    IonItemGroup,
    IonLabel,
    IonNote,
  ],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/more" text="Ще" /></ion-buttons>
        <ion-title>Про застосунок</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content [fullscreen]="true">
      <ion-list [inset]="true">
        <ion-item-group>
          <ion-item>
            <ion-label>Версія</ion-label>
            <ion-note slot="end">{{ version }}</ion-note>
          </ion-item>
          <ion-item>
            <ion-label>Відкрито як</ion-label>
            <ion-note slot="end">{{ standalone() ? 'застосунок' : 'вкладка браузера' }}</ion-note>
          </ion-item>
          <ion-item>
            <ion-label>Режим Ionic</ion-label>
            <ion-note slot="end">{{ mode }}</ion-note>
          </ion-item>
          <ion-item>
            <ion-label>Платформа Capacitor</ion-label>
            <ion-note slot="end">{{ nativePlatform }}</ion-note>
          </ion-item>
          <ion-item>
            <ion-label class="ion-text-wrap">
              <h3>Платформи Ionic</h3>
              <p>{{ platforms }}</p>
            </ion-label>
          </ion-item>
          <ion-item>
            <ion-label class="ion-text-wrap">
              <h3>Сервер</h3>
              <p>{{ apiUrl }}</p>
            </ion-label>
          </ion-item>
        </ion-item-group>
      </ion-list>

      <ion-card>
        <ion-card-header>
          <ion-card-title>Перевірка оболонки</ion-card-title>
        </ion-card-header>
        <ion-card-content>
          <p>Натиснуто: {{ taps() }} · подвоєно: {{ doubled() }}</p>
          <ion-button expand="block" fill="outline" (click)="tap()">Натиснути</ion-button>
        </ion-card-content>
      </ion-card>
    </ion-content>
  `,
})
export class AboutPage {
  protected readonly taps = signal(0);
  protected readonly doubled = computed(() => this.taps() * 2);

  protected readonly mode = document.documentElement.getAttribute('mode') ?? '?';
  protected readonly nativePlatform = Capacitor.getPlatform();
  protected readonly platforms = getPlatforms().join(', ');
  protected readonly apiUrl = apiUrl();
  protected readonly version = environment.version;
  protected readonly standalone = inject(InstallService).standalone;

  protected async tap(): Promise<void> {
    this.taps.update((n) => n + 1);
    // Native shells use the Taptic engine; on the web Capacitor falls back to navigator.vibrate
    // (Android browsers), and iOS Safari has no vibration API at all.
    try {
      await Haptics.impact({ style: ImpactStyle.Light });
    } catch {
      // No haptics on this device.
    }
  }
}
