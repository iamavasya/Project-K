import { Component } from '@angular/core';
import { IonBackButton, IonButtons, IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular';

/** Placeholder until the feature lands (PLAN.md §19). */
@Component({
  selector: 'app-event-form',
  imports: [IonBackButton, IonButtons, IonContent, IonHeader, IonTitle, IonToolbar],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/calendar" /></ion-buttons>
        <ion-title>Нова подія</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content [fullscreen]="true">
      <p class="ion-padding">Скоро.</p>
    </ion-content>
  `,
})
export class EventFormPage {}
