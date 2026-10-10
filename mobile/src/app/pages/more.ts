import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {
  ActionSheetController,
  IonAvatar,
  IonContent,
  IonHeader,
  IonIcon,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonNote,
  IonTitle,
  IonToolbar,
  ViewWillEnter,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { informationCircle, shieldCheckmark } from 'ionicons/icons';
import { AuthService } from '../auth/auth.service';
import { initials } from '../me/labels';
import { MemberDto } from '../me/me.models';
import { MeService } from '../me/me.service';

/**
 * Laid out as iOS Settings: the account row on top (avatar, name, what it opens), then rows with
 * icon tiles, and the sign-out on its own at the end.
 */
@Component({
  selector: 'app-more',
  imports: [
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonList,
    IonItem,
    IonItemGroup,
    IonLabel,
    IonNote,
    IonAvatar,
    IonIcon,
    RouterLink,
  ],
  styles: `
    .account ion-avatar {
      width: 60px;
      height: 60px;
      margin: 12px 16px 12px 0;
      background: var(--lk-primary);
      color: var(--lk-on-primary);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 24px;
      font-weight: 700;
    }
    .sign-out {
      text-align: center;
    }
    .account h2 {
      font-size: 20px;
      font-weight: 700;
      color: var(--lk-ink);
    }
  `,
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
        <ion-list [inset]="true" class="account">
          <ion-item-group>
            <ion-item [button]="true" [detail]="true" routerLink="profile" data-testid="account">
              <ion-avatar slot="start" aria-hidden="true">
                @if (member()?.profilePhotoUrl; as photo) {
                  <img [src]="photo" alt="" />
                } @else {
                  {{ monogram() }}
                }
              </ion-avatar>
              <ion-label class="ion-text-wrap">
                <h2>{{ name() || me.email }}</h2>
                <p>{{ name() ? 'Профіль, ' + me.email : 'Профіль' }}</p>
              </ion-label>
            </ion-item>
          </ion-item-group>
        </ion-list>
      }
      <ion-list [inset]="true">
        <ion-item-group>
          @if (user()) {
            <ion-item [button]="true" [detail]="true" routerLink="/mfa">
              <ion-icon class="lk-tile" slot="start" name="shield-checkmark" aria-hidden="true" style="--lk-tile: #34a853" />
              <ion-label>Двофакторний вхід</ion-label>
              <ion-note slot="end">{{ mfaLabel() }}</ion-note>
            </ion-item>
          }
          <ion-item [button]="true" [detail]="true" routerLink="about">
            <ion-icon class="lk-tile" slot="start" name="information-circle" aria-hidden="true" style="--lk-tile: #8e9a95" />
            <ion-label>Про застосунок</ion-label>
          </ion-item>
        </ion-item-group>
      </ion-list>
      @if (user()) {
        <ion-list [inset]="true">
          <ion-item-group>
            <ion-item [button]="true" [detail]="false" [disabled]="leaving()" (click)="confirmLogout()">
              <ion-label class="sign-out" color="danger">Вийти</ion-label>
            </ion-item>
          </ion-item-group>
        </ion-list>
      }
    </ion-content>
  `,
})
export class MorePage implements ViewWillEnter {
  private readonly auth = inject(AuthService);
  private readonly me = inject(MeService);
  private readonly router = inject(Router);
  private readonly sheets = inject(ActionSheetController);
  protected readonly user = this.auth.user;
  protected readonly member = signal<MemberDto | null>(null);
  protected readonly leaving = signal(false);
  protected readonly name = computed(() => {
    const m = this.member();
    return m ? `${m.firstName} ${m.lastName}`.trim() : '';
  });
  protected readonly monogram = computed(() => {
    const m = this.member();
    return m ? initials(m.firstName, m.lastName) : (this.user()?.email.charAt(0).toUpperCase() ?? '');
  });
  protected readonly mfaLabel = computed(() => {
    const enabled = this.auth.mfaEnabled();
    if (enabled === null) return '';
    return enabled ? 'увімкнено' : 'вимкнено';
  });

  constructor() {
    addIcons({ shieldCheckmark, informationCircle });
  }

  /** The status and the card are asked once (the status endpoint is rate-limited). */
  async ionViewWillEnter(): Promise<void> {
    const user = this.user();
    if (!user) return;
    if (user.memberKey && !this.member()) {
      this.me.member(user.memberKey).then(
        (member) => this.member.set(member),
        () => undefined, // The row keeps the email.
      );
    }
    if (this.auth.mfaEnabled() !== null) return;
    try {
      await this.auth.mfaStatus();
    } catch {
      // The row just shows no status.
    }
  }

  /** As iOS Settings does: a sheet with the destructive action and a way back. */
  protected async confirmLogout(): Promise<void> {
    const sheet = await this.sheets.create({
      header: 'Вийти з акаунта на цьому пристрої?',
      buttons: [
        { text: 'Вийти', role: 'destructive', data: 'leave' },
        { text: 'Скасувати', role: 'cancel' },
      ],
    });
    await sheet.present();
    const { data } = await sheet.onWillDismiss();
    if (data === 'leave') await this.logout();
  }

  private async logout(): Promise<void> {
    this.leaving.set(true);
    await this.auth.logout();
    this.leaving.set(false);
    await this.router.navigateByUrl('/login', { replaceUrl: true });
  }
}
