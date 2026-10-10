import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  IonBackButton,
  IonButton,
  IonButtons,
  IonCard,
  IonCardContent,
  IonContent,
  IonHeader,
  IonIcon,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonListHeader,
  IonNote,
  IonRefresher,
  IonRefresherContent,
  IonSearchbar,
  IonSkeletonText,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { alertCircle, checkmarkCircle, people, trophy, wallet } from 'ionicons/icons';
import { AuthService } from '../../auth/auth.service';
import { FAILED_TEXT, Loaded, settle, valueOf } from '../../core/loaded';
import {
  activeWarning,
  birthdayWhen,
  currentOffices,
  filterMembers,
  fullName,
  kurinAccess,
  plastLevelLabel,
  roleName,
  shortDate,
  sortMembers,
  upcomingBirthdays,
  warningText,
} from './kurin.labels';
import { GroupDto, LeadershipHistoryDto, MemberLookupDto } from './kurin.models';
import { KurinApi } from './kurin.service';
import { MemberAvatar } from './member-avatar';

const DESCRIPTION_LIMIT = 220;
const BIRTHDAY_DAYS = 30;
const BIRTHDAY_PREVIEW = 5;
/** Below this many people a search field is more clutter than help. */
const SEARCH_FROM = 9;

/**
 * A гурток (web group-panel): its silhouette and description, провід, birthdays in the next 30 days
 * and its people as mini cards, with rows to its score and — for those who keep its box — its dues.
 * Editing the profile, the silhouette, people and виховники stays on the web.
 */
@Component({
  selector: 'app-group',
  imports: [
    IonHeader,
    IonToolbar,
    IonButtons,
    IonBackButton,
    IonButton,
    IonTitle,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    IonCard,
    IonCardContent,
    IonList,
    IonListHeader,
    IonItem,
    IonItemGroup,
    IonLabel,
    IonNote,
    IonIcon,
    IonSearchbar,
    IonSkeletonText,
    RouterLink,
    MemberAvatar,
  ],
  styles: `
    .hero {
      display: flex;
      gap: 14px;
      align-items: center;
    }
    .silhouette {
      flex: none;
      width: 72px;
      height: 72px;
      border-radius: 16px;
      background: var(--lk-primary-50);
      color: var(--lk-primary);
      display: flex;
      align-items: center;
      justify-content: center;
      overflow: hidden;
      font-size: 36px;
    }
    .silhouette img {
      width: 100%;
      height: 100%;
      object-fit: contain;
    }
    .eyebrow {
      margin: 0;
      font-size: 12px;
      font-weight: 700;
      letter-spacing: 0.12em;
      text-transform: uppercase;
      color: var(--lk-faint);
    }
    .hero h2 {
      margin: 2px 0;
      font-size: 22px;
      font-weight: 700;
      color: var(--lk-ink);
    }
    .hero p {
      margin: 0;
      color: var(--lk-muted);
    }
    .description {
      margin: 14px 0 0;
      color: var(--lk-ink-soft);
      white-space: pre-line;
    }
    .description.collapsed {
      display: -webkit-box;
      -webkit-line-clamp: 4;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .verified {
      color: var(--lk-primary);
      font-size: 16px;
      vertical-align: -3px;
      margin-inline-start: 4px;
    }
    .stale {
      color: var(--lk-faint);
    }
    .dots {
      display: inline-flex;
      gap: 3px;
      margin-inline-start: 6px;
      vertical-align: 2px;
    }
    .dots span {
      width: 7px;
      height: 7px;
      border-radius: 50%;
      background: var(--lk-line);
    }
    .dots span.on {
      background: var(--lk-danger);
    }
    .today {
      color: var(--lk-danger);
      font-weight: 600;
    }
    ion-searchbar {
      padding-inline: 12px;
    }
    app-member-avatar {
      margin-inline-end: 12px;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/kurin" text="Курінь" /></ion-buttons>
        <ion-title>{{ groupValue()?.name ?? 'Гурток' }}</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" (ionRefresh)="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>

      <ion-card data-testid="group-head">
        <ion-card-content>
          @switch (group().state) {
            @case ('loading') { <ion-skeleton-text [animated]="true" style="height: 72px" /> }
            @case ('failed') { <p>{{ failedText }}</p> }
            @default {
              @if (groupValue(); as g) {
                <div class="hero">
                  <div class="silhouette">
                    @if (g.silhouetteUrl) {
                      <img [src]="g.silhouetteUrl" [alt]="'Сильветка гуртка ' + g.name" />
                    } @else {
                      <ion-icon name="people" aria-hidden="true" />
                    }
                  </div>
                  <div>
                    <p class="eyebrow">Гурток</p>
                    <h2>{{ g.name }}</h2>
                    @if (g.kurinNumber) { <p>{{ g.kurinNumber }} курінь</p> }
                  </div>
                </div>
                @if (description(); as text) {
                  <p class="description" [class.collapsed]="longDescription() && !expanded()">{{ text }}</p>
                  @if (longDescription()) {
                    <ion-button fill="clear" size="small" (click)="expanded.set(!expanded())">
                      {{ expanded() ? 'Згорнути' : 'Розгорнути' }}
                    </ion-button>
                  }
                }
              }
            }
          }
        </ion-card-content>
      </ion-card>

      <ion-list [inset]="true">
        <ion-item-group>
          <ion-item [button]="true" [detail]="true" [routerLink]="['/tabs/kurin/group', groupKey, 'score']">
            <ion-icon class="lk-tile" slot="start" name="trophy" aria-hidden="true" style="--lk-tile: #f2a900" />
            <ion-label>Точкування гуртка</ion-label>
          </ion-item>
          @if (keepsDues()) {
            <ion-item [button]="true" [detail]="true" [routerLink]="['/tabs/kurin/group', groupKey, 'dues']" data-testid="group-dues">
              <ion-icon class="lk-tile" slot="start" name="wallet" aria-hidden="true" style="--lk-tile: #30b0c7" />
              <ion-label>Вкладка гуртка</ion-label>
            </ion-item>
          }
        </ion-item-group>
      </ion-list>

      <ion-list [inset]="true" data-testid="group-leadership">
        <ion-list-header><ion-label>Провід гуртка</ion-label></ion-list-header>
        <ion-item-group>
          @switch (leadership().state) {
            @case ('loading') { <ion-item><ion-skeleton-text [animated]="true" style="height: 24px" /></ion-item> }
            @case ('failed') { <ion-item><ion-label>Проводу ще немає.</ion-label></ion-item> }
            @default {
              @for (office of offices(); track office.leadershipHistoryKey) {
                <ion-item [button]="true" [detail]="true" [routerLink]="['/tabs/kurin/member', office.member.memberKey]">
                  <ion-label class="ion-text-wrap">
                    <h3>{{ name(office.member) }}</h3>
                    <p>{{ roleName(office.role) }}</p>
                  </ion-label>
                </ion-item>
              } @empty {
                <ion-item><ion-label>Проводу ще немає.</ion-label></ion-item>
              }
            }
          }
        </ion-item-group>
      </ion-list>

      @if (birthdays().length) {
        <ion-list [inset]="true" data-testid="birthdays">
          <ion-list-header><ion-label>Найближчі дні народження</ion-label></ion-list-header>
          <ion-item-group>
            @for (birthday of birthdays().slice(0, birthdayPreview); track birthday.member.memberKey) {
              <ion-item [button]="true" [detail]="true" [routerLink]="['/tabs/kurin/member', birthday.member.memberKey]">
                <app-member-avatar slot="start" [photo]="birthday.member.profilePhotoUrl" [firstName]="birthday.member.firstName" [lastName]="birthday.member.lastName" />
                <ion-label>
                  <h3>{{ birthday.member.firstName }} {{ birthday.member.lastName }}</h3>
                  <p>{{ shortDate(birthday.date) }}</p>
                </ion-label>
                <ion-note slot="end" [class.today]="birthday.daysUntil === 0">{{ birthdayWhen(birthday.daysUntil) }}</ion-note>
              </ion-item>
            }
            @if (birthdays().length > birthdayPreview) {
              <ion-item>
                <ion-label class="ion-text-wrap"><p>Ще {{ birthdays().length - birthdayPreview }} у межах 30 днів.</p></ion-label>
              </ion-item>
            }
          </ion-item-group>
        </ion-list>
      }

      @if (memberCount() >= searchFrom) {
        <ion-searchbar
          placeholder="Пошук"
          [value]="query()"
          [debounce]="150"
          (ionInput)="query.set($any($event).detail.value ?? '')"
        />
      }
      <ion-list [inset]="true" data-testid="group-members">
        <ion-list-header><ion-label>Учасники гуртка</ion-label></ion-list-header>
        <ion-item-group>
          @switch (members().state) {
            @case ('loading') {
              @for (i of [1, 2, 3]; track i) {
                <ion-item><ion-skeleton-text [animated]="true" style="height: 40px" /></ion-item>
              }
            }
            @case ('failed') { <ion-item><ion-label class="ion-text-wrap">{{ failedText }}</ion-label></ion-item> }
            @default {
              @for (member of shownMembers(); track member.memberKey) {
                <ion-item [button]="true" [detail]="true" [routerLink]="['/tabs/kurin/member', member.memberKey]" data-testid="group-member">
                  <app-member-avatar slot="start" [size]="44" [photo]="member.profilePhotoUrl" [firstName]="member.firstName" [lastName]="member.lastName" />
                  <ion-label class="ion-text-wrap">
                    <h3>
                      {{ member.firstName }} {{ member.lastName }}
                      @if (member.profileVerificationStatus === 'VerifiedCurrent') {
                        <ion-icon class="verified" name="checkmark-circle" aria-label="Дані верифіковано" />
                      } @else if (member.profileVerificationStatus === 'VerifiedStale') {
                        <ion-icon class="verified stale" name="alert-circle" aria-label="Дані змінено після верифікації" />
                      }
                      @if (warningOf(member); as warning) {
                        <span class="dots" role="img" [attr.aria-label]="warningText(warning)">
                          @for (level of [1, 2, 3]; track level) { <span [class.on]="level <= warning.level"></span> }
                        </span>
                      }
                    </h3>
                    <p>{{ levelOf(member) }}</p>
                  </ion-label>
                </ion-item>
              } @empty {
                <ion-item><ion-label>{{ query() ? 'Нічого не знайшлось.' : 'У гуртку ще нікого немає.' }}</ion-label></ion-item>
              }
            }
          }
        </ion-item-group>
      </ion-list>
    </ion-content>
  `,
})
export class GroupPage implements OnInit {
  private readonly data = inject(KurinApi);
  private readonly auth = inject(AuthService);

  protected readonly groupKey = inject(ActivatedRoute).snapshot.paramMap.get('groupKey') ?? '';

  protected readonly failedText = FAILED_TEXT;
  protected readonly roleName = roleName;
  protected readonly shortDate = shortDate;
  protected readonly birthdayWhen = birthdayWhen;
  protected readonly warningText = warningText;
  protected readonly birthdayPreview = BIRTHDAY_PREVIEW;
  protected readonly searchFrom = SEARCH_FROM;

  protected readonly expanded = signal(false);
  protected readonly query = signal('');
  protected readonly group = signal<Loaded<GroupDto>>({ state: 'loading' });
  protected readonly leadership = signal<Loaded<LeadershipHistoryDto[]>>({ state: 'loading' });
  protected readonly members = signal<Loaded<MemberLookupDto[]>>({ state: 'loading' });
  private readonly duesGroups = signal<string[]>([]);

  protected readonly groupValue = computed(() => valueOf(this.group()));
  protected readonly description = computed(() => this.groupValue()?.description?.trim() ?? '');
  protected readonly longDescription = computed(() => this.description().length > DESCRIPTION_LIMIT);
  protected readonly offices = computed(() => currentOffices(valueOf(this.leadership())));
  protected readonly memberCount = computed(() => valueOf(this.members())?.length ?? 0);
  protected readonly shownMembers = computed(() => sortMembers(filterMembers(valueOf(this.members()) ?? [], this.query()), 'name'));
  protected readonly birthdays = computed(() => upcomingBirthdays(valueOf(this.members()) ?? [], BIRTHDAY_DAYS));
  /** As the web's menu: the «Вкладка гуртка» entry is there for whoever keeps this гурток's box. */
  protected readonly keepsDues = computed(() => this.duesGroups().includes(this.groupKey));

  constructor() {
    addIcons({ people, trophy, wallet, checkmarkCircle, alertCircle });
  }

  ngOnInit(): void {
    void this.load();
  }

  protected async refresh(event: Event): Promise<void> {
    await this.load();
    await (event.target as HTMLIonRefresherElement).complete();
  }

  protected name(member: MemberLookupDto): string {
    return fullName(member);
  }

  protected levelOf(member: MemberLookupDto): string {
    return plastLevelLabel(member) ?? 'Ступінь не вказано';
  }

  protected warningOf(member: MemberLookupDto): { level: number; daysLeft: number } | null {
    return activeWarning(member.warnings);
  }

  private async load(): Promise<void> {
    const groupKey = this.groupKey;
    const kurinKey = this.auth.user()?.kurinKey;
    await Promise.all([
      settle(this.data.group(groupKey), this.group),
      settle(this.data.groupMembers(groupKey), this.members),
      settle(this.data.leadership('group', groupKey).then((l) => l?.leadershipHistories ?? []), this.leadership),
      kurinKey && kurinAccess(this.auth.user()).seeGroupDues
        ? this.data.duesGroups(kurinKey).then(
            (groups) => this.duesGroups.set(groups.map((g) => g.groupKey)),
            () => this.duesGroups.set([]),
          )
        : Promise.resolve(),
    ]);
  }
}
