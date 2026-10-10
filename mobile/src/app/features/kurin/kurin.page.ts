import { Component, ElementRef, OnDestroy, OnInit, afterRenderEffect, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  ActionSheetController,
  IonBadge,
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
  IonRefresher,
  IonRefresherContent,
  IonSearchbar,
  IonSegment,
  IonSegmentButton,
  IonSkeletonText,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { alertCircle, checkmarkCircle, people, ribbon, swapVertical, trophy } from 'ionicons/icons';
import { AuthService } from '../../auth/auth.service';
import { FAILED_TEXT, Loaded, settle, valueOf } from '../../core/loaded';
import { GlassEffects } from '../../ui/glass';
import {
  BRANCH_LABELS,
  KvRow,
  MemberSort,
  currentOffices,
  filterMembers,
  fullName,
  hasYouthProgram,
  kurinAccess,
  kvRows,
  memberRoleTags,
  plastLevelLabel,
  roleName,
  sortMembers,
} from './kurin.labels';
import { GroupDto, KurinDto, LeadershipHistoryDto, MemberLookupDto } from './kurin.models';
import { KurinApi } from './kurin.service';
import { MemberAvatar } from './member-avatar';

type Section = 'overview' | 'members';

const DESCRIPTION_LIMIT = 360;

/**
 * The kurin tab (web kurin-panel): who the kurin is, its гуртки, КВ and провід on «Огляд», and
 * everyone in it on «Учасники». Read-only: creating groups, adding people, the PDF report and the
 * КВ and провід forms stay on the web.
 */
@Component({
  selector: 'app-kurin',
  imports: [
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonButton,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    IonSegment,
    IonSegmentButton,
    IonLabel,
    IonCard,
    IonCardContent,
    IonList,
    IonListHeader,
    IonItem,
    IonItemGroup,
    IonIcon,
    IonBadge,
    IonSearchbar,
    IonSkeletonText,
    RouterLink,
    MemberAvatar,
  ],
  styles: `
    .segment {
      padding: 4px 16px 8px;
    }
    .head {
      display: flex;
      gap: 14px;
      align-items: center;
    }
    .number {
      flex: none;
      width: 56px;
      height: 56px;
      border-radius: 14px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--lk-primary);
      color: var(--lk-on-primary);
      font-size: 24px;
      font-weight: 800;
    }
    .head h2 {
      margin: 0;
      font-size: 20px;
      font-weight: 700;
      color: var(--lk-ink);
    }
    .head p {
      margin: 2px 0 0;
      color: var(--lk-muted);
    }
    .facts {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 8px;
      margin: 14px 0 0;
    }
    .facts dt {
      font-size: 13px;
      font-weight: 600;
      color: var(--lk-faint);
    }
    .facts dd {
      margin: 2px 0 0;
      color: var(--lk-ink);
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
    ion-badge {
      margin-top: 6px;
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
    ion-searchbar {
      padding-inline: 12px;
    }
    .empty {
      padding: 4px 20px;
      color: var(--lk-muted);
    }
    app-member-avatar {
      margin-inline-end: 12px;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-title>Курінь</ion-title>
        @if (section() === 'members') {
          <ion-buttons slot="end">
            <ion-button aria-label="Сортування" (click)="chooseSort()">
              <ion-icon slot="icon-only" name="swap-vertical" />
            </ion-button>
          </ion-buttons>
        }
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" (ionRefresh)="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>
      <ion-header collapse="condense">
        <ion-toolbar><ion-title size="large">Курінь</ion-title></ion-toolbar>
      </ion-header>

      @if (!kurinKey) {
        <p class="empty">Ти ще не в курені.</p>
      } @else {
        <div class="segment">
          <ion-segment [value]="section()" (ionChange)="pick($event)">
            <ion-segment-button value="overview"><ion-label>Огляд</ion-label></ion-segment-button>
            <ion-segment-button value="members"><ion-label>Учасники</ion-label></ion-segment-button>
          </ion-segment>
        </div>

        @if (section() === 'overview') {
          <ion-card data-testid="kurin-head">
            <ion-card-content>
              @switch (kurin().state) {
                @case ('loading') { <ion-skeleton-text [animated]="true" style="height: 56px" /> }
                @case ('failed') { <p>{{ failedText }}</p> }
                @default {
                  @if (kurinValue(); as k) {
                    <div class="head">
                      <div class="number" aria-hidden="true">{{ k.number }}</div>
                      <div>
                        <h2>{{ k.number }} курінь</h2>
                        @if (k.namedAfter) { <p>ім. {{ k.namedAfter }}</p> }
                        <ion-badge class="lk-tag--secondary">{{ branchLabel(k) }}</ion-badge>
                      </div>
                    </div>
                    <dl class="facts">
                      <div><dt>Станиця</dt><dd>{{ k.stanytsia || '—' }}</dd></div>
                      <div><dt>Край</dt><dd>{{ k.regionOrCountry || '—' }}</dd></div>
                    </dl>
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
              <ion-item [button]="true" [detail]="true" routerLink="/tabs/kurin/score">
                <ion-icon class="lk-tile" slot="start" name="trophy" aria-hidden="true" style="--lk-tile: #f2a900" />
                <ion-label>Точкування</ion-label>
              </ion-item>
              @if (canReviewSkills()) {
                <ion-item [button]="true" [detail]="true" routerLink="/tabs/kurin/review/skills">
                  <ion-icon class="lk-tile" slot="start" name="ribbon" aria-hidden="true" style="--lk-tile: #5e5ce6" />
                  <ion-label>Вмілості на перевірку</ion-label>
                </ion-item>
              }
            </ion-item-group>
          </ion-list>

          <ion-list [inset]="true" data-testid="groups">
            <ion-list-header><ion-label>Гуртки</ion-label></ion-list-header>
            <ion-item-group>
              @switch (groups().state) {
                @case ('loading') { <ion-item><ion-skeleton-text [animated]="true" style="height: 24px" /></ion-item> }
                @case ('failed') { <ion-item><ion-label class="ion-text-wrap">{{ failedText }}</ion-label></ion-item> }
                @default {
                  @for (group of groupList(); track group.groupKey) {
                    <ion-item [button]="true" [detail]="true" [routerLink]="['/tabs/kurin/group', group.groupKey]">
                      <ion-icon class="lk-tile" slot="start" name="people" aria-hidden="true" style="--lk-tile: #0e6e4e" />
                      <ion-label>{{ group.name }}</ion-label>
                    </ion-item>
                  } @empty {
                    <ion-item><ion-label>Гуртків поки немає.</ion-label></ion-item>
                  }
                }
              }
            </ion-item-group>
          </ion-list>

          <ion-list [inset]="true" data-testid="kv">
            <ion-list-header><ion-label>КВ</ion-label></ion-list-header>
            <ion-item-group>
              @switch (kv().state) {
                @case ('loading') { <ion-item><ion-skeleton-text [animated]="true" style="height: 24px" /></ion-item> }
                @case ('failed') { <ion-item><ion-label class="ion-text-wrap">{{ failedText }}</ion-label></ion-item> }
                @default {
                  @for (row of kvList(); track row.member.memberKey) {
                    <ion-item [button]="true" [detail]="true" [routerLink]="['/tabs/kurin/member', row.member.memberKey]">
                      <app-member-avatar slot="start" [photo]="row.member.profilePhotoUrl" [firstName]="row.member.firstName" [lastName]="row.member.lastName" />
                      <ion-label class="ion-text-wrap">
                        <h3>{{ name(row.member) }}</h3>
                        <p>{{ row.status }} · {{ row.groups.length ? row.groups.join(', ') : 'без гуртків' }}</p>
                      </ion-label>
                    </ion-item>
                  } @empty {
                    <ion-item><ion-label>Впорядників ще не призначено.</ion-label></ion-item>
                  }
                }
              }
            </ion-item-group>
          </ion-list>

          <ion-list [inset]="true" data-testid="leadership">
            <ion-list-header><ion-label>Провід куреня</ion-label></ion-list-header>
            <ion-item-group>
              @switch (leadership().state) {
                @case ('loading') { <ion-item><ion-skeleton-text [animated]="true" style="height: 24px" /></ion-item> }
                @case ('failed') { <ion-item><ion-label class="ion-text-wrap">Проводу ще немає.</ion-label></ion-item> }
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
        } @else {
          <ion-searchbar
            placeholder="Пошук"
            [value]="query()"
            [debounce]="150"
            (ionInput)="query.set($any($event).detail.value ?? '')"
            data-testid="member-search"
          />
          <ion-list [inset]="true" data-testid="members">
            <ion-list-header><ion-label>{{ membersHeader() }}</ion-label></ion-list-header>
            <ion-item-group>
              @switch (members().state) {
                @case ('loading') {
                  @for (i of [1, 2, 3, 4]; track i) {
                    <ion-item><ion-skeleton-text [animated]="true" style="height: 32px" /></ion-item>
                  }
                }
                @case ('failed') { <ion-item><ion-label class="ion-text-wrap">{{ failedText }}</ion-label></ion-item> }
                @default {
                  @for (member of shownMembers(); track member.memberKey) {
                    <ion-item [button]="true" [detail]="true" [routerLink]="['/tabs/kurin/member', member.memberKey]" data-testid="member">
                      <app-member-avatar slot="start" [photo]="member.profilePhotoUrl" [firstName]="member.firstName" [lastName]="member.lastName" />
                      <ion-label class="ion-text-wrap">
                        <h3>
                          {{ name(member) }}
                          @if (member.profileVerificationStatus === 'VerifiedCurrent') {
                            <ion-icon class="verified" name="checkmark-circle" aria-label="Дані верифіковано" />
                          } @else if (member.profileVerificationStatus === 'VerifiedStale') {
                            <ion-icon class="verified stale" name="alert-circle" aria-label="Дані змінено після верифікації" />
                          }
                        </h3>
                        <p>{{ memberLine(member) }}</p>
                      </ion-label>
                    </ion-item>
                  } @empty {
                    <ion-item><ion-label>{{ query() ? 'Нічого не знайшлось.' : 'Тут ще нікого немає.' }}</ion-label></ion-item>
                  }
                }
              }
            </ion-item-group>
          </ion-list>
        }
      }
    </ion-content>
  `,
})
export class KurinPage implements OnInit, OnDestroy {
  private readonly data = inject(KurinApi);
  private readonly auth = inject(AuthService);
  private readonly sheets = inject(ActionSheetController);
  private readonly glass = new GlassEffects(inject<ElementRef<HTMLElement>>(ElementRef).nativeElement);

  protected readonly kurinKey = this.auth.user()?.kurinKey ?? null;
  protected readonly failedText = FAILED_TEXT;
  protected readonly roleName = roleName;

  protected readonly section = signal<Section>('overview');
  protected readonly expanded = signal(false);
  protected readonly query = signal('');
  protected readonly sort = signal<MemberSort>('name');

  protected readonly kurin = signal<Loaded<KurinDto>>({ state: 'loading' });
  protected readonly groups = signal<Loaded<GroupDto[]>>({ state: 'loading' });
  protected readonly kv = signal<Loaded<KvRow[]>>({ state: 'loading' });
  protected readonly leadership = signal<Loaded<LeadershipHistoryDto[]>>({ state: 'loading' });
  protected readonly members = signal<Loaded<MemberLookupDto[]>>({ state: 'loading' });

  protected readonly kurinValue = computed(() => valueOf(this.kurin()));
  protected readonly groupList = computed(() => valueOf(this.groups()) ?? []);
  protected readonly kvList = computed(() => valueOf(this.kv()) ?? []);
  protected readonly offices = computed(() => currentOffices(valueOf(this.leadership())));
  protected readonly description = computed(() => this.kurinValue()?.description?.trim() ?? '');
  protected readonly longDescription = computed(() => this.description().length > DESCRIPTION_LIMIT);
  /** As the web's menu: reviewers of a youth kurin. */
  protected readonly canReviewSkills = computed(
    () => kurinAccess(this.auth.user()).reviewSkills && hasYouthProgram(this.kurinValue()?.branch),
  );
  protected readonly shownMembers = computed(() =>
    sortMembers(filterMembers(valueOf(this.members()) ?? [], this.query()), this.sort()),
  );
  protected readonly membersHeader = computed(() => {
    const all = valueOf(this.members());
    const by = this.sort() === 'name' ? 'за прізвищем' : 'за посадою';
    return all ? `${all.length} ${plural(all.length)} · ${by}` : 'Учасники';
  });

  constructor() {
    addIcons({ trophy, ribbon, people, swapVertical, checkmarkCircle, alertCircle });
    afterRenderEffect(() => {
      this.section();
      this.glass.sync();
    });
  }

  ngOnInit(): void {
    void this.load();
  }

  ngOnDestroy(): void {
    this.glass.destroy();
  }

  protected pick(event: Event): void {
    const value = (event as CustomEvent<{ value?: string }>).detail.value;
    if (value === 'overview' || value === 'members') this.section.set(value);
  }

  protected async refresh(event: Event): Promise<void> {
    await this.load();
    await (event.target as HTMLIonRefresherElement).complete();
  }

  protected name(member: MemberLookupDto): string {
    return fullName(member);
  }

  protected branchLabel(kurin: KurinDto): string {
    return BRANCH_LABELS[kurin.branch ?? 'UPYu'];
  }

  /** The offices first, as the web's «Статус» column; else the ступінь. */
  protected memberLine(member: MemberLookupDto): string {
    const roles = memberRoleTags(member);
    if (roles.length) return roles.join(' · ');
    return plastLevelLabel(member) ?? 'Учасник';
  }

  protected async chooseSort(): Promise<void> {
    const sheet = await this.sheets.create({
      header: 'Сортування',
      buttons: [
        { text: 'За прізвищем', data: 'name' },
        { text: 'За посадою', data: 'role' },
        { text: 'Скасувати', role: 'cancel' },
      ],
    });
    await sheet.present();
    const { data } = await sheet.onWillDismiss<MemberSort>();
    if (data === 'name' || data === 'role') this.sort.set(data);
  }

  private async load(): Promise<void> {
    const kurinKey = this.kurinKey;
    if (!kurinKey) return;
    const groups = this.data.groups(kurinKey);
    await Promise.all([
      settle(this.data.kurin(kurinKey), this.kurin),
      settle(groups, this.groups),
      settle(this.data.kurinMembers(kurinKey), this.members),
      settle(
        this.data.leadership('kurin', kurinKey).then((l) => l?.leadershipHistories ?? []),
        this.leadership,
      ),
      settle(
        Promise.all([groups, this.data.kvMembers(kurinKey), this.data.mentorAssignments(kurinKey)]).then(
          ([all, kv, assignments]) => kvRows(kv, assignments, all),
        ),
        this.kv,
      ),
    ]);
  }
}

function plural(count: number): string {
  const tens = count % 100;
  const ones = count % 10;
  if (tens >= 11 && tens <= 14) return 'учасників';
  if (ones === 1) return 'учасник';
  if (ones >= 2 && ones <= 4) return 'учасники';
  return 'учасників';
}
