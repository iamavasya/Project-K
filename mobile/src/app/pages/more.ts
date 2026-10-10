import { Component } from '@angular/core';
import {
  IonContent,
  IonHeader,
  IonItem,
  IonLabel,
  IonList,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';

@Component({
  selector: 'app-more',
  imports: [IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonItem, IonLabel],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-title>Ще</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content [fullscreen]="true">
      <ion-header collapse="condense">
        <ion-toolbar>
          <ion-title size="large">Ще</ion-title>
        </ion-toolbar>
      </ion-header>
      <ion-list [inset]="true">
        <ion-item [button]="true" [detail]="true"><ion-label>Профіль</ion-label></ion-item>
        <ion-item [button]="true" [detail]="true"><ion-label>Вигляд</ion-label></ion-item>
        <ion-item [button]="true" [detail]="true"><ion-label>Про Лілейку</ion-label></ion-item>
      </ion-list>
    </ion-content>
  `,
})
export class MorePage {}
