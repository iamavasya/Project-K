import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  IonBackButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonListHeader,
  IonRefresher,
  IonRefresherContent,
  IonSkeletonText,
  IonSpinner,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { checkmark, people } from 'ionicons/icons';
import { KurinScopeOption } from '../../auth/auth.models';
import { AuthService } from '../../auth/auth.service';
import { apiErrorText } from '../../core/api';
import { FAILED_TEXT, Loaded, settle, valueOf } from '../../core/loaded';
import { Toasts } from '../../core/toast';
import { BRANCH_LABELS, KIND_LABELS, groupRole, kurinLabel, sortKurins } from './account.labels';
import { MyGroupDto } from './account.models';
import { AccountService } from './account.service';
import { GroupSilhouette } from '../kurin/group-silhouette';

/**
 * «Мої курені»: the web's kurin switcher and my-kurins tile. The kurin acted in is checked; picking
 * another asks the server for a token scoped to it, and the app opens again on Головна, so every
 * tab reads the new kurin from scratch. A гурток of another kurin switches first, then opens.
 */
@Component({
  selector: 'app-kurins',
  imports: [
    GroupSilhouette,
    IonHeader,
    IonToolbar,
    IonButtons,
    IonBackButton,
    IonTitle,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    IonList,
    IonListHeader,
    IonItemGroup,
    IonItem,
    IonLabel,
    IonIcon,
    IonSpinner,
    IonSkeletonText,
  ],
  styles: `
    .plaque {
      flex: none;
      width: 40px;
      height: 40px;
      margin: 8px 16px 8px 0;
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--lk-primary-50);
      color: var(--lk-primary-700);
      font-weight: 800;
      font-size: 16px;
    }
    .plaque-silhouette {
      margin: 8px 16px 8px 0;
    }
    .plaque ion-icon {
      font-size: 20px;
    }
    .now {
      color: var(--lk-primary);
      font-size: 22px;
    }
    .note {
      padding: 16px 20px;
      color: var(--lk-muted);
    }
    .footnote {
      margin: -8px 32px 16px;
      font-size: 13px;
      line-height: 18px;
      color: var(--lk-muted);
    }
    :host-context(.md) .footnote {
      margin: 0 20px 16px;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/more" text="Ще" /></ion-buttons>
        <ion-title>Мої курені</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" (ionRefresh)="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>

      @switch (options().state) {
        @case ('loading') {
          <ion-list [inset]="true">
            <ion-item-group>
              @for (row of [1, 2]; track row) {
                <ion-item><ion-skeleton-text [animated]="true" style="width: 60%; height: 20px" /></ion-item>
              }
            </ion-item-group>
          </ion-list>
        }
        @case ('failed') {
          <p class="note">{{ failedText }}</p>
        }
        @default {
          @if (kurins().length) {
            <ion-list [inset]="true">
              <ion-list-header><ion-label>Курені</ion-label></ion-list-header>
              <ion-item-group>
                @for (option of kurins(); track option.kurinKey) {
                  <ion-item
                    [button]="!isCurrent(option)"
                    [detail]="false"
                    [disabled]="switching() !== null && switching() !== option.kurinKey"
                    (click)="choose(option)"
                    [attr.data-testid]="'kurin-' + option.kurinNumber"
                  >
                    <span class="plaque" slot="start" aria-hidden="true">{{ option.kurinNumber }}</span>
                    <ion-label class="ion-text-wrap">
                      <h3>{{ label(option) }}</h3>
                      <p>{{ branch[option.branch] }} · {{ kind[option.kind] }}</p>
                    </ion-label>
                    @if (switching() === option.kurinKey) {
                      <ion-spinner slot="end" name="crescent" />
                    } @else if (isCurrent(option)) {
                      <ion-icon class="now" slot="end" name="checkmark" aria-label="Поточний курінь" />
                    }
                  </ion-item>
                }
              </ion-item-group>
            </ion-list>
            @if (kurins().length > 1) {
              <p class="footnote">Курінь, у якому ти зараз дієш. Права залежать від куреня, тож після перемикання Лілейка відкриється заново.</p>
            }
          } @else {
            <p class="note">Поки ти не стоїш у жодному курені. Провід приймає за кодом з твоєї картки.</p>
          }

          @if (groups().length) {
            <ion-list [inset]="true">
              <ion-list-header><ion-label>Гуртки</ion-label></ion-list-header>
              <ion-item-group>
                @for (group of groups(); track group.groupKey) {
                  <ion-item
                    [button]="true"
                    [detail]="true"
                    [disabled]="switching() !== null"
                    (click)="openGroup(group)"
                    [attr.data-testid]="'group-' + group.groupKey"
                  >
                    <app-group-silhouette slot="start" class="plaque-silhouette" [url]="group.silhouetteUrl" [size]="40" />
                    <ion-label class="ion-text-wrap">
                      <h3>{{ group.name }}</h3>
                      <p>{{ role(group) }}@if (kurins().length > 1) { · к. ч. {{ group.kurin.kurinNumber }} }</p>
                    </ion-label>
                    @if (switching() === group.groupKey) {
                      <ion-spinner slot="end" name="crescent" />
                    }
                  </ion-item>
                }
              </ion-item-group>
            </ion-list>
          }
        }
      }
    </ion-content>
  `,
})
export class KurinsPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly service = inject(AccountService);
  private readonly router = inject(Router);
  private readonly toasts = inject(Toasts);
  protected readonly failedText = FAILED_TEXT;
  protected readonly branch = BRANCH_LABELS;
  protected readonly kind = KIND_LABELS;
  protected readonly label = kurinLabel;
  protected readonly role = groupRole;

  protected readonly options = signal<Loaded<KurinScopeOption[]>>({ state: 'loading' });
  private readonly groupList = signal<MyGroupDto[]>([]);
  /** The kurin or гурток being opened; every row waits while the token switches. */
  protected readonly switching = signal<string | null>(null);
  private readonly currentKey = computed(() => this.auth.user()?.kurinKey ?? null);
  protected readonly kurins = computed(() => sortKurins(valueOf(this.options()) ?? [], this.currentKey()));
  protected readonly groups = computed(() =>
    [...this.groupList()].sort(
      (a, b) => Number(b.kurin.isCurrent) - Number(a.kurin.isCurrent) || a.kurin.kurinNumber - b.kurin.kurinNumber,
    ),
  );

  constructor() {
    addIcons({ checkmark, people });
  }

  ngOnInit(): void {
    void this.load();
  }

  protected async refresh(event: Event): Promise<void> {
    await this.load();
    await (event.target as HTMLIonRefresherElement).complete();
  }

  protected isCurrent(option: KurinScopeOption): boolean {
    return option.kurinKey === this.currentKey();
  }

  protected async choose(option: KurinScopeOption): Promise<void> {
    if (this.isCurrent(option) || this.switching()) return;
    await this.switchTo(option.kurinKey, option.kurinNumber, option.kurinKey, 'tabs/home');
  }

  protected async openGroup(group: MyGroupDto): Promise<void> {
    if (this.switching()) return;
    const path = `tabs/kurin/group/${group.groupKey}`;
    if (group.kurin.kurinKey === this.currentKey()) {
      await this.router.navigateByUrl(`/${path}`);
      return;
    }
    await this.switchTo(group.kurin.kurinKey, group.kurin.kurinNumber, group.groupKey, path);
  }

  /**
   * The page under every tab belonged to the previous kurin; its keys open nothing in the new one.
   * So the app starts again at `path` on the new token (the refresh cookie was rotated with it).
   */
  private async switchTo(kurinKey: string, kurinNumber: number, rowKey: string, path: string): Promise<void> {
    this.switching.set(rowKey);
    try {
      await this.auth.setKurinScope(kurinKey);
      globalThis.location.replace(new URL(path, document.baseURI).href);
    } catch (error) {
      this.switching.set(null);
      await this.toasts.show(
        apiErrorText(error, `Курінь ч. ${kurinNumber} лишився недосяжним. Спробуй ще раз.`),
        'danger',
      );
    }
  }

  private async load(): Promise<void> {
    await Promise.all([
      settle(this.auth.kurinScopeOptions(), this.options),
      this.service.groups().then(
        (groups) => this.groupList.set(groups),
        () => undefined, // The kurins still show; гуртки are a shortcut.
      ),
    ]);
  }
}
