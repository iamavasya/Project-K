import { Component, ElementRef, OnInit, computed, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  ActionSheetController,
  Config,
  IonBackButton,
  IonButton,
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
  ModalController,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { createOutline, personAdd, swapHorizontal } from 'ionicons/icons';
import { AuthService } from '../../auth/auth.service';
import { apiErrorText } from '../../core/api';
import { FAILED_TEXT, Loaded, settle, valueOf } from '../../core/loaded';
import { Toasts } from '../../core/toast';
import { KvRow, fullName, kurinAccess, kvArchive, kvRowTags, kvRows, toDateOnly, utcDate } from './kurin.labels';
import { GroupDto, MemberLookupDto, MentorAssignmentDto } from './kurin.models';
import { KurinApi } from './kurin.service';
import { KvMentorSheet } from './kv-mentor.sheet';
import { handoverRequest, officePeriod } from './leadership-seats';
import { MemberAvatar } from './member-avatar';
import { PeoplePickSheet, PickPerson } from './people-pick.sheet';
import { RoleTags } from './role-tags';

interface KvData {
  groups: GroupDto[];
  rows: KvRow[];
  archive: MentorAssignmentDto[];
}

/**
 * The kurin's КВ (web kv-panel): the Звʼязковий and the впорядники with their гуртки, and the
 * archive of ended assignments — everyone reads both. The Звʼязковий and admins (web
 * canManageMentors) add a впорядник, change whose гуртки are whose, and hand the Звʼязковий's
 * office to someone else.
 */
@Component({
  selector: 'app-kv',
  imports: [
    RoleTags,
    IonHeader,
    IonToolbar,
    IonButtons,
    IonBackButton,
    IonButton,
    IonTitle,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    IonList,
    IonListHeader,
    IonItem,
    IonItemGroup,
    IonLabel,
    IonIcon,
    IonSkeletonText,
    IonSpinner,
    RouterLink,
    MemberAvatar,
  ],
  styles: `
    app-member-avatar {
      margin-inline-end: 12px;
    }
    .actions {
      margin: 0;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/kurin" [text]="backText" /></ion-buttons>
        <ion-title>КВ</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" (ionRefresh)="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>

      <ion-list [inset]="true" data-testid="kv-people">
        <ion-list-header><ion-label>Звʼязковий і впорядники</ion-label></ion-list-header>
        <ion-item-group>
          @switch (kv().state) {
            @case ('loading') {
              @for (i of [1, 2]; track i) {
                <ion-item><ion-skeleton-text [animated]="true" style="height: 40px" /></ion-item>
              }
            }
            @case ('failed') { <ion-item><ion-label class="ion-text-wrap">{{ failedText }}</ion-label></ion-item> }
            @default {
              @for (row of rows(); track row.member.memberKey) {
                <ion-item [button]="true" [detail]="!manager()" [routerLink]="['/tabs/kurin/member', row.member.memberKey]" data-testid="kv-row">
                  <app-member-avatar slot="start" [photo]="row.member.profilePhotoUrl" [firstName]="row.member.firstName" [lastName]="row.member.lastName" />
                  <ion-label class="ion-text-wrap">
                    <h3>{{ name(row.member) }}</h3>
                    <app-role-tags [tags]="tagsOf(row)" />
                  </ion-label>
                  @if (manager() && row.member.userKey) {
                    <ion-button
                      slot="end"
                      fill="clear"
                      size="default"
                      class="actions"
                      [disabled]="busy()"
                      [attr.aria-label]="'Налаштувати гуртки: ' + row.member.firstName + ' ' + row.member.lastName"
                      (click)="$event.stopPropagation(); $event.preventDefault(); editMentor(row)"
                      data-testid="edit-mentor"
                    >
                      <ion-icon slot="icon-only" name="create-outline" aria-hidden="true" />
                    </ion-button>
                  }
                </ion-item>
              } @empty {
                <ion-item><ion-label class="ion-text-wrap">У КВ поки нікого. Призначені впорядники зʼявляться тут.</ion-label></ion-item>
              }
              @if (manager()) {
                <ion-item [button]="true" [detail]="false" [disabled]="busy() || candidates().state !== 'ready'" (click)="editMentor(null)" data-testid="add-mentor">
                  <ion-icon slot="start" name="person-add" color="primary" aria-hidden="true" />
                  <ion-label color="primary">Додати до КВ</ion-label>
                </ion-item>
              }
            }
          }
        </ion-item-group>
      </ion-list>

      @if (manager() && kv().state === 'ready') {
        <ion-list [inset]="true">
          <ion-item-group>
            <ion-item [button]="true" [detail]="false" [disabled]="busy() || candidates().state !== 'ready'" (click)="handOver()" data-testid="hand-over">
              @if (busy()) {
                <ion-spinner slot="start" name="crescent" />
              } @else {
                <ion-icon slot="start" name="swap-horizontal" color="primary" aria-hidden="true" />
              }
              <ion-label color="primary">Передати роль Звʼязкового</ion-label>
            </ion-item>
          </ion-item-group>
        </ion-list>
      }

      @if (kv().state === 'ready') {
        <ion-list [inset]="true" data-testid="kv-archive">
          <ion-list-header><ion-label>Архів впорядників</ion-label></ion-list-header>
          <ion-item-group>
            @for (item of archive(); track item.mentorAssignmentKey) {
              @if (item.member) {
                <ion-item [button]="true" [detail]="true" [routerLink]="['/tabs/kurin/member', item.member.memberKey]" data-testid="kv-archived">
                  <ion-label class="ion-text-wrap">
                    <h3>{{ name(item.member) }}</h3>
                    <p>{{ archivePeriod(item) }}</p>
                    <app-role-tags [tags]="[{ label: item.groupName, tone: 'secondary' }]" />
                  </ion-label>
                </ion-item>
              } @else {
                <ion-item data-testid="kv-archived">
                  <ion-label class="ion-text-wrap">
                    <h3>Невідомий учасник</h3>
                    <p>{{ archivePeriod(item) }}</p>
                    <app-role-tags [tags]="[{ label: item.groupName, tone: 'secondary' }]" />
                  </ion-label>
                </ion-item>
              }
            } @empty {
              <ion-item><ion-label>Архів призначень порожній.</ion-label></ion-item>
            }
          </ion-item-group>
        </ion-list>
      }
    </ion-content>
  `,
})
export class KvPage implements OnInit {
  private readonly data = inject(KurinApi);
  private readonly auth = inject(AuthService);
  private readonly modals = inject(ModalController);
  private readonly sheets = inject(ActionSheetController);
  private readonly toasts = inject(Toasts);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;

  private readonly kurinKey = this.auth.user()?.kurinKey ?? '';
  protected readonly backText = inject(Config).get('mode') === 'ios' ? 'Курінь' : undefined;
  protected readonly failedText = FAILED_TEXT;

  protected readonly kv = signal<Loaded<KvData>>({ state: 'loading' });
  /** The kurin's people with an account, read only for those who manage the КВ. */
  protected readonly candidates = signal<Loaded<MemberLookupDto[]>>({ state: 'loading' });
  protected readonly busy = signal(false);
  /** Web canManageMentors: the Звʼязковий and admins. */
  protected readonly manager = computed(() => kurinAccess(this.auth.user()).manageWholeKurin);
  protected readonly rows = computed(() => valueOf(this.kv())?.rows ?? []);
  protected readonly archive = computed(() => valueOf(this.kv())?.archive ?? []);

  constructor() {
    addIcons({ createOutline, personAdd, swapHorizontal });
    // A гурток's виховники changed on its page, or here: the КВ follows.
    effect(() => {
      if (this.data.groupsChanged()) untracked(() => void this.loadKv());
    });
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

  /** «01.09.24 – 31.08.25» for an assignment (web 'dd.MM.yy', in the reader's zone). */
  protected readonly tagsOf = kvRowTags;

  protected archivePeriod(item: MentorAssignmentDto): string {
    const day = (value: string | null) => {
      const date = utcDate(value);
      return date ? toDateOnly(date) : null;
    };
    return officePeriod(day(item.assignedAtUtc), day(item.revokedAtUtc));
  }

  /** «Учасник КВ»: from a row, their гуртки; with none, someone new to the КВ. */
  protected async editMentor(row: KvRow | null): Promise<void> {
    const kv = valueOf(this.kv());
    if (!kv || this.busy()) return;
    const modal = await this.modals.create({
      component: KvMentorSheet,
      componentProps: { candidates: valueOf(this.candidates()) ?? [], groups: kv.groups, rows: kv.rows, row, kurinKey: this.kurinKey },
      presentingElement: this.presenting(),
    });
    await modal.present();
  }

  /**
   * «Передати роль Звʼязкового» (web kv-panel): the office moves, not a system role — access comes
   * from the office, so the КВ's провід is written back with the new holder.
   */
  protected async handOver(): Promise<void> {
    if (this.busy()) return;
    const current = this.rows().find((r) => r.isManager)?.member ?? null;
    const people = (valueOf(this.candidates()) ?? []).filter(
      (c) => !!c.userKey && !(current && (c.userKey === current.userKey || c.memberKey === current.memberKey)),
    );
    const modal = await this.modals.create({
      component: PeoplePickSheet,
      componentProps: {
        title: 'Новий Звʼязковий',
        people: people.map((member): PickPerson => ({ key: member.memberKey, member })),
        emptyText: 'У курені ще нікого з акаунтом немає.',
        hint: 'Права Звʼязкового перейдуть до обраного учасника.',
      },
      presentingElement: this.presenting(),
    });
    await modal.present();
    const { data } = await modal.onWillDismiss<string[] | null>();
    const incoming = people.find((p) => p.memberKey === data?.[0]);
    if (!incoming) return;

    const confirm = await this.sheets.create({
      header: 'Передати роль Звʼязкового?',
      subHeader: `${incoming.firstName} ${incoming.lastName} стане Звʼязковим куреня.`,
      buttons: [
        { text: 'Передати', role: 'destructive', data: 'yes' },
        { text: 'Скасувати', role: 'cancel' },
      ],
    });
    await confirm.present();
    if ((await confirm.onWillDismiss()).data !== 'yes') return;

    this.busy.set(true);
    try {
      const kvLeadership = (await this.data.leadership('kv', this.kurinKey)) ?? null;
      await this.data.saveLeadership(
        kvLeadership?.leadershipKey ?? null,
        handoverRequest(kvLeadership, this.kurinKey, incoming, toDateOnly(new Date())),
      );
      await this.rereadRights(incoming);
      await this.toasts.show(`Роль Звʼязкового передано: ${incoming.firstName} ${incoming.lastName}.`);
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося передати роль. Спробуй ще раз.'), 'danger');
    } finally {
      this.busy.set(false);
    }
    await this.load();
  }

  /**
   * Rights come from the offices when a token is minted, so whoever gave the office away (unless an
   * admin) takes a fresh one for the same kurin; the screens then stop offering what is no longer
   * theirs. A failure only leaves the old rights until the next sign-in, as on the web.
   */
  private async rereadRights(incoming: MemberLookupDto): Promise<void> {
    const me = this.auth.user();
    if (!me || me.isAdmin || me.userKey === incoming.userKey) return;
    try {
      await this.auth.setKurinScope(this.kurinKey);
    } catch {
      // Kept as they were; the server still refuses what the office no longer allows.
    }
  }

  private presenting(): HTMLElement | undefined {
    return this.host.closest<HTMLElement>('.ion-page') ?? undefined;
  }

  private loadKv(): Promise<void> {
    const kurinKey = this.kurinKey;
    return settle(
      Promise.all([this.data.groups(kurinKey), this.data.kvMembers(kurinKey), this.data.mentorAssignments(kurinKey)]).then(
        ([groups, members, assignments]): KvData => ({ groups, rows: kvRows(members, assignments, groups), archive: kvArchive(assignments) }),
      ),
      this.kv,
    );
  }

  private async load(): Promise<void> {
    if (!this.kurinKey) return;
    await Promise.all([this.loadKv(), this.manager() ? settle(this.data.mentorCandidates(this.kurinKey), this.candidates) : Promise.resolve()]);
  }
}
