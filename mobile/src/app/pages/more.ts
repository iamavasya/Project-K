import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  IonContent,
  IonHeader,
  IonItem,
  IonLabel,
  IonList,
  IonListHeader,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-more',
  imports: [IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonListHeader, IonItem, IonLabel],
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
      @if (user(); as me) {
        <ion-list [inset]="true">
          <ion-list-header>Акаунт</ion-list-header>
          <ion-item>
            <ion-label class="ion-text-wrap">
              <h3>Email</h3>
              <p>{{ me.email }}</p>
            </ion-label>
          </ion-item>
        </ion-list>
      }
      <ion-list [inset]="true">
        <ion-item [button]="true" [detail]="true"><ion-label>Профіль</ion-label></ion-item>
        <ion-item [button]="true" [detail]="true"><ion-label>Вигляд</ion-label></ion-item>
        <ion-item [button]="true" [detail]="true"><ion-label>Про Лілейку</ion-label></ion-item>
      </ion-list>
      @if (user()) {
        <ion-list [inset]="true">
          <ion-item [button]="true" [disabled]="leaving()" (click)="logout()">
            <ion-label color="danger">Вийти</ion-label>
          </ion-item>
        </ion-list>
      }
    </ion-content>
  `,
})
export class MorePage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly user = this.auth.user;
  protected readonly leaving = signal(false);

  protected async logout(): Promise<void> {
    this.leaving.set(true);
    await this.auth.logout();
    await this.router.navigateByUrl('/login', { replaceUrl: true });
  }
}
