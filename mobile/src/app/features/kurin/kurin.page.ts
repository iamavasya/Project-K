import { Component } from '@angular/core';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular';

/** Placeholder until the feature lands (PLAN.md §19). */
@Component({
  selector: 'app-kurin',
  imports: [IonContent, IonHeader, IonTitle, IonToolbar],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-title>Курінь</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content [fullscreen]="true">
      <ion-header collapse="condense">
        <ion-toolbar><ion-title size="large">Курінь</ion-title></ion-toolbar>
      </ion-header>
      <p class="ion-padding">Скоро.</p>
    </ion-content>
  `,
})
export class KurinPage {}
