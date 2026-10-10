import { Component } from '@angular/core';
import { IonButtons, IonContent, IonHeader, IonMenuButton, IonTitle, IonToolbar } from '@ionic/angular';
import { HeaderActions } from '../nav/header-actions';
import { NavMenu } from '../nav/nav-menu';

/** The «Меню» tab: the web's sidebar, item for item (nav/nav-menu), under the web's toolbar actions. */
@Component({
  selector: 'app-more',
  imports: [IonHeader, IonToolbar, IonTitle, IonButtons, IonMenuButton, IonContent, HeaderActions, NavMenu],
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-menu-button /></ion-buttons>
        <ion-title>Меню</ion-title>
        <ion-buttons slot="end"><app-header-actions /></ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content [fullscreen]="true">
      <ion-header collapse="condense">
        <ion-toolbar>
          <ion-title size="large">Меню</ion-title>
        </ion-toolbar>
      </ion-header>
      <app-nav-menu />
    </ion-content>
  `,
})
export class MorePage {}
