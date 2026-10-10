import { Component, computed, inject, signal } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { Haptics, ImpactStyle } from '@capacitor/haptics';
import {
  IonButton,
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardTitle,
  IonContent,
  IonHeader,
  IonItem,
  IonLabel,
  IonList,
  IonNote,
  IonTitle,
  IonToolbar,
  getPlatforms,
} from '@ionic/angular';
import { environment } from '../../environments/environment';
import { InstallCard } from '../pwa/install-card';
import { InstallService } from '../pwa/install.service';

@Component({
  selector: 'app-home',
  imports: [
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonButton,
    IonList,
    IonItem,
    IonLabel,
    IonNote,
    InstallCard,
  ],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-title>Лілейка</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true">
      <ion-header collapse="condense">
        <ion-toolbar>
          <ion-title size="large">Лілейка</ion-title>
        </ion-toolbar>
      </ion-header>

      <app-install-card />

      <ion-card>
        <ion-card-header>
          <ion-card-title>Привіт від S1</ion-card-title>
        </ion-card-header>
        <ion-card-content>
          <p>Ionic 9 + Angular 22 (zoneless) + Capacitor 8.</p>
          <p>Натиснуто: {{ taps() }} · подвоєно: {{ doubled() }}</p>
          <ion-button expand="block" (click)="tap()">Натиснути</ion-button>
        </ion-card-content>
      </ion-card>

      <ion-list [inset]="true">
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
            <h3>API</h3>
            <p>{{ apiUrl }}</p>
          </ion-label>
        </ion-item>
        <ion-item>
          <ion-label>Відкрито як</ion-label>
          <ion-note slot="end">{{ standalone() ? 'застосунок' : 'вкладка браузера' }}</ion-note>
        </ion-item>
        <ion-item>
          <ion-label>Версія</ion-label>
          <ion-note slot="end">{{ version }}</ion-note>
        </ion-item>
      </ion-list>
    </ion-content>
  `,
})
export class HomePage {
  protected readonly taps = signal(0);
  protected readonly doubled = computed(() => this.taps() * 2);

  protected readonly mode = document.documentElement.getAttribute('mode') ?? '?';
  protected readonly nativePlatform = Capacitor.getPlatform();
  protected readonly platforms = getPlatforms().join(', ');
  protected readonly apiUrl = environment.apiUrl;
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
