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
import {
  bug,
  colorPalette,
  flag,
  helpCircle,
  informationCircle,
  lockClosed,
  openOutline,
  personCircle,
  shieldCheckmark,
} from 'ionicons/icons';
import { KurinScopeOption } from '../auth/auth.models';
import { AuthService } from '../auth/auth.service';
import { kurinShortLabel } from '../features/account/account.labels';
import { AppearanceService } from '../features/account/appearance.service';
import { helpUrl } from '../features/account/web-links';
import { initials } from '../me/labels';
import { MemberDto } from '../me/me.models';
import { MeService } from '../me/me.service';

const THEME_LABELS = { system: 'Системна', light: 'Світла', dark: 'Темна' } as const;

/**
 * Laid out as iOS Settings: the account row on top (avatar, name, what it opens), then grouped rows
 * with icon tiles (the account and its kurins, the look, help and the app), and the sign-out on its
 * own at the end.
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
    .external {
      font-size: 18px;
      color: var(--lk-faint);
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

        <ion-list [inset]="true">
          <ion-item-group>
            <ion-item [button]="true" [detail]="true" routerLink="account">
              <ion-icon class="lk-tile" slot="start" name="person-circle" aria-hidden="true" style="--lk-tile: #007aff" />
              <ion-label>Акаунт</ion-label>
            </ion-item>
            <ion-item [button]="true" [detail]="true" routerLink="account" data-testid="mfa-row">
              <ion-icon class="lk-tile" slot="start" name="shield-checkmark" aria-hidden="true" style="--lk-tile: #34a853" />
              <ion-label>Двофакторний вхід</ion-label>
              <ion-note slot="end">{{ mfaLabel() }}</ion-note>
            </ion-item>
            @if (kurins().length > 1) {
              <ion-item [button]="true" [detail]="true" routerLink="kurins" data-testid="kurins-row">
                <ion-icon class="lk-tile" slot="start" name="flag" aria-hidden="true" style="--lk-tile: #ff9500" />
                <ion-label>Мої курені</ion-label>
                <ion-note slot="end">{{ currentKurin() }}</ion-note>
              </ion-item>
            } @else if (kurins().length === 1) {
              <!-- Nothing to choose from: the kurin is shown, not offered (the web hides its switcher). -->
              <ion-item data-testid="kurins-row">
                <ion-icon class="lk-tile" slot="start" name="flag" aria-hidden="true" style="--lk-tile: #ff9500" />
                <ion-label>Курінь</ion-label>
                <ion-note slot="end">{{ currentKurin() }}</ion-note>
              </ion-item>
            }
          </ion-item-group>
        </ion-list>
      }

      <ion-list [inset]="true">
        <ion-item-group>
          <ion-item [button]="true" [detail]="true" routerLink="appearance">
            <ion-icon class="lk-tile" slot="start" name="color-palette" aria-hidden="true" style="--lk-tile: #5856d6" />
            <ion-label>Вигляд</ion-label>
            <ion-note slot="end">{{ themeLabel() }}</ion-note>
          </ion-item>
        </ion-item-group>
      </ion-list>

      <ion-list [inset]="true">
        <ion-item-group>
          <ion-item [href]="help" target="_blank" rel="noopener" [detail]="false" data-testid="help">
            <ion-icon class="lk-tile" slot="start" name="help-circle" aria-hidden="true" style="--lk-tile: #0a84ff" />
            <ion-label>Довідка</ion-label>
            <ion-icon class="external" slot="end" name="open-outline" aria-hidden="true" />
          </ion-item>
          @if (user()) {
            <ion-item [button]="true" [detail]="true" routerLink="report">
              <ion-icon class="lk-tile" slot="start" name="bug" aria-hidden="true" style="--lk-tile: #ff3b30" />
              <ion-label>Повідомити про проблему</ion-label>
            </ion-item>
          }
          <ion-item [button]="true" [detail]="true" routerLink="privacy">
            <ion-icon class="lk-tile" slot="start" name="lock-closed" aria-hidden="true" style="--lk-tile: #30b0c7" />
            <ion-label>Конфіденційність</ion-label>
          </ion-item>
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
  private readonly appearance = inject(AppearanceService);
  protected readonly help = helpUrl();
  protected readonly user = this.auth.user;
  protected readonly member = signal<MemberDto | null>(null);
  protected readonly kurins = signal<KurinScopeOption[]>([]);
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
  protected readonly currentKurin = computed(() => {
    const key = this.user()?.kurinKey;
    const current = this.kurins().find((option) => option.kurinKey === key);
    return current ? kurinShortLabel(current) : '';
  });
  protected readonly themeLabel = computed(() => THEME_LABELS[this.appearance.choice()]);

  constructor() {
    addIcons({
      personCircle,
      shieldCheckmark,
      flag,
      colorPalette,
      helpCircle,
      bug,
      lockClosed,
      informationCircle,
      openOutline,
    });
  }

  /** The status, the card and the kurins are asked once (the status endpoint is rate-limited). */
  async ionViewWillEnter(): Promise<void> {
    const user = this.user();
    if (!user) return;
    if (user.memberKey && !this.member()) {
      this.me.member(user.memberKey).then(
        (member) => this.member.set(member),
        () => undefined, // The row keeps the email.
      );
    }
    if (!this.kurins().length) {
      this.auth.kurinScopeOptions().then(
        (options) => this.kurins.set(options),
        () => undefined, // No row: the choice is a convenience, as on the web.
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
