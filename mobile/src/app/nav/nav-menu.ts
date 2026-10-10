import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import {
  ActionSheetController,
  IonAvatar,
  IonIcon,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonNote,
  MenuController,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import {
  book,
  bug,
  calendar,
  checkbox,
  cloudUpload,
  colorPalette,
  eyeOff,
  flag,
  grid,
  home,
  informationCircle,
  key,
  openOutline,
  options,
  people,
  person,
  ribbon,
  settings,
  shieldCheckmark,
  time,
  trophy,
  wallet,
} from 'ionicons/icons';
import { filter, map } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { kurinShortLabel } from '../features/account/account.labels';
import { AppearanceService } from '../features/account/appearance.service';
import { helpUrl, openExternal, webPage } from '../features/account/web-links';
import { hasYouthProgram, kurinAccess } from '../features/kurin/kurin.labels';
import { KurinApi } from '../features/kurin/kurin.service';
import { initials } from '../me/labels';
import { MemberDto } from '../me/me.models';
import { MeService } from '../me/me.service';
import { KurinScopes } from './kurin-scopes.service';
import { MenuEntry, currentEntry, menuEntries } from './menu-items';

const THEME_LABELS = { system: 'Системна', light: 'Світла', dark: 'Темна' } as const;

/**
 * The web's sidebar as a list: the person on top (the web's «Мій профіль» and its footer with the
 * email), then the sidebar's items in its order, then what only the phone has (the look, the
 * kurins, privacy) and the sign-out. The «Меню» tab shows it, and so does the ☰ drawer.
 */
@Component({
  selector: 'app-nav-menu',
  imports: [IonList, IonItem, IonItemGroup, IonLabel, IonNote, IonAvatar, IonIcon],
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
    .account h2 {
      font-size: 20px;
      font-weight: 700;
      color: var(--lk-ink);
    }
    .current {
      --background: var(--lk-primary-50);
      font-weight: 600;
    }
    .external {
      font-size: 18px;
      color: var(--lk-faint);
    }
    .sign-out {
      text-align: center;
    }
  `,
  template: `
    @if (user(); as me) {
      <ion-list [inset]="true" class="account">
        <ion-item-group>
          <ion-item
            [button]="true"
            [detail]="true"
            [class.current]="current() === 'profile'"
            (click)="open('/tabs/more/profile')"
            data-testid="account"
          >
            <ion-avatar slot="start" aria-hidden="true">
              @if (member()?.profilePhotoUrl; as photo) {
                <img [src]="photo" alt="" />
              } @else {
                {{ monogram() }}
              }
            </ion-avatar>
            <ion-label class="ion-text-wrap">
              <h2>{{ name() || me.email }}</h2>
              <p>{{ name() ? 'Мій профіль, ' + me.email : 'Мій профіль' }}</p>
            </ion-label>
          </ion-item>
        </ion-item-group>
      </ion-list>
    }

    <ion-list [inset]="true" data-testid="web-menu">
      <ion-item-group>
        @for (entry of entries(); track entry.key) {
          <ion-item
            [button]="true"
            [detail]="!!entry.link"
            [class.current]="current() === entry.key"
            (click)="go(entry)"
            [attr.data-testid]="'menu-' + entry.key"
          >
            <ion-icon class="lk-tile" slot="start" [name]="entry.icon" aria-hidden="true" [style.--lk-tile]="entry.tile" />
            <ion-label>{{ entry.label }}</ion-label>
            @if (!entry.link) {
              <ion-icon class="external" slot="end" name="open-outline" aria-hidden="true" />
            }
          </ion-item>
        }
      </ion-item-group>
    </ion-list>

    <ion-list [inset]="true">
      <ion-item-group>
        <ion-item [button]="true" [detail]="true" [class.current]="current() === 'appearance'" (click)="open('/tabs/more/appearance')">
          <ion-icon class="lk-tile" slot="start" name="color-palette" aria-hidden="true" style="--lk-tile: #5856d6" />
          <ion-label>Вигляд</ion-label>
          <ion-note slot="end">{{ themeLabel() }}</ion-note>
        </ion-item>
        @if (scopes.options().length > 1) {
          <ion-item
            [button]="true"
            [detail]="true"
            [class.current]="current() === 'kurins'"
            (click)="open('/tabs/more/kurins')"
            data-testid="kurins-row"
          >
            <ion-icon class="lk-tile" slot="start" name="flag" aria-hidden="true" style="--lk-tile: #ff9500" />
            <ion-label>Мої курені</ion-label>
            <ion-note slot="end">{{ currentKurin() }}</ion-note>
          </ion-item>
        }
        <ion-item [button]="true" [detail]="true" [class.current]="current() === 'privacy'" (click)="open('/tabs/more/privacy')">
          <ion-icon class="lk-tile" slot="start" name="eye-off" aria-hidden="true" style="--lk-tile: #30b0c7" />
          <ion-label>Конфіденційність</ion-label>
        </ion-item>
      </ion-item-group>
    </ion-list>

    @if (user()) {
      <ion-list [inset]="true">
        <ion-item-group>
          <ion-item [button]="true" [detail]="false" [disabled]="leaving()" (click)="confirmLogout()" data-testid="sign-out">
            <ion-label class="sign-out" color="danger">Вийти</ion-label>
          </ion-item>
        </ion-item-group>
      </ion-list>
    }
  `,
})
export class NavMenu implements OnInit {
  /** In the ☰ drawer: a tap also closes it. */
  readonly drawer = input(false);

  private readonly auth = inject(AuthService);
  private readonly me = inject(MeService);
  private readonly kurinApi = inject(KurinApi);
  private readonly router = inject(Router);
  private readonly menus = inject(MenuController);
  private readonly sheets = inject(ActionSheetController);
  private readonly appearance = inject(AppearanceService);
  protected readonly scopes = inject(KurinScopes);
  protected readonly user = this.auth.user;
  protected readonly member = signal<MemberDto | null>(null);
  private readonly duesGroups = signal<{ groupKey: string; groupName: string }[]>([]);
  protected readonly leaving = signal(false);

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  protected readonly entries = computed(() =>
    menuEntries(this.user(), {
      // Unknown until the kurins come: the web shows the item rather than hide one the person may use.
      youthKurin: hasYouthProgram(this.scopes.current()?.branch ?? null),
      duesGroups: this.duesGroups(),
    }).filter((entry) => entry.key !== 'profile'),
  );
  protected readonly current = computed(() => {
    const url = this.url();
    const own = ['appearance', 'kurins', 'privacy'].find((page) => url.startsWith(`/tabs/more/${page}`));
    if (own) return own;
    if (url.startsWith('/tabs/more/profile')) return 'profile';
    return currentEntry(this.entries(), url);
  });
  protected readonly name = computed(() => {
    const m = this.member();
    return m ? `${m.firstName} ${m.lastName}`.trim() : '';
  });
  protected readonly monogram = computed(() => {
    const m = this.member();
    return m ? initials(m.firstName, m.lastName) : (this.user()?.email.charAt(0).toUpperCase() ?? '');
  });
  protected readonly currentKurin = computed(() => {
    const current = this.scopes.current();
    return current ? kurinShortLabel(current) : '';
  });
  protected readonly themeLabel = computed(() => THEME_LABELS[this.appearance.choice()]);

  constructor() {
    addIcons({
      book,
      bug,
      calendar,
      checkbox,
      cloudUpload,
      colorPalette,
      eyeOff,
      flag,
      grid,
      home,
      informationCircle,
      key,
      openOutline,
      options,
      people,
      person,
      ribbon,
      settings,
      shieldCheckmark,
      time,
      trophy,
      wallet,
    });
  }

  ngOnInit(): void {
    const user = this.user();
    if (!user) return;
    void this.scopes.load();
    if (user.memberKey) {
      this.me.member(user.memberKey).then(
        (member) => this.member.set(member),
        () => undefined, // The row keeps the email.
      );
    }
    if (user.kurinKey && kurinAccess(user).seeGroupDues) {
      this.kurinApi.duesGroups(user.kurinKey).then(
        (groups) => this.duesGroups.set(groups),
        () => undefined, // As on the web: a failed read hides the item.
      );
    }
  }

  protected async go(entry: MenuEntry): Promise<void> {
    if (entry.link) {
      await this.open(entry.link);
      return;
    }
    openExternal(entry.help ? helpUrl() : webPage(entry.web ?? '/'));
    if (this.drawer()) await this.menus.close();
  }

  protected async open(link: string): Promise<void> {
    if (this.drawer()) await this.menus.close();
    await this.router.navigateByUrl(link);
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
    if (data !== 'leave') return;
    this.leaving.set(true);
    if (this.drawer()) await this.menus.close();
    await this.auth.logout();
    this.leaving.set(false);
    await this.router.navigateByUrl('/login', { replaceUrl: true });
  }
}
