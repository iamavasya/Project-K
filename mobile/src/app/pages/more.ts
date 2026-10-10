import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {
  IonContent,
  IonHeader,
  IonItem,
  IonLabel,
  IonList,
  IonListHeader,
  IonNote,
  IonTitle,
  IonToolbar,
  ViewWillEnter,
} from '@ionic/angular';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-more',
  imports: [IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonListHeader, IonItem, IonLabel, IonNote, RouterLink],
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
          <ion-item [button]="true" [detail]="true" routerLink="profile">
            <ion-label class="ion-text-wrap">
              <h3>Профіль</h3>
              <p>{{ me.email }}</p>
            </ion-label>
          </ion-item>
          <ion-item [button]="true" [detail]="true" routerLink="/mfa">
            <ion-label>Двофакторний вхід</ion-label>
            <ion-note slot="end">{{ mfaLabel() }}</ion-note>
          </ion-item>
        </ion-list>
      }
      <ion-list [inset]="true">
        <ion-item [button]="true" [detail]="true" routerLink="about">
          <ion-label>Про застосунок</ion-label>
        </ion-item>
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
export class MorePage implements ViewWillEnter {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly user = this.auth.user;
  protected readonly leaving = signal(false);
  protected readonly mfaLabel = computed(() => {
    const enabled = this.auth.mfaEnabled();
    return enabled === null ? '' : enabled ? 'увімкнено' : 'вимкнено';
  });

  /** The status is asked once (the endpoint is rate-limited); turning it on updates it in place. */
  async ionViewWillEnter(): Promise<void> {
    if (!this.user() || this.auth.mfaEnabled() !== null) return;
    try {
      await this.auth.mfaStatus();
    } catch {
      // The row just shows no status.
    }
  }

  protected async logout(): Promise<void> {
    this.leaving.set(true);
    await this.auth.logout();
    this.leaving.set(false);
    await this.router.navigateByUrl('/login', { replaceUrl: true });
  }
}
