import { Component, ElementRef, OnInit, computed, inject, signal } from '@angular/core';
import {
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
  IonNote,
  IonSpinner,
  IonTitle,
  IonToolbar,
  ModalController,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { checkmarkCircle, ellipseOutline } from 'ionicons/icons';
import { apiErrorText } from '../../core/api';
import { Toasts } from '../../core/toast';
import { mentorChanges } from './group-people';
import { KvRow, fullName } from './kurin.labels';
import { GroupDto, MemberLookupDto } from './kurin.models';
import { KurinApi } from './kurin.service';
import { PeoplePickSheet, PickPerson } from './people-pick.sheet';

/**
 * «Налаштування впорядника» (web kv-panel's dialog): a member of the kurin with an account and the
 * гуртки they are виховник of. Opened empty it adds a впорядник; from a row it changes their
 * гуртки. Saving assigns the newly ticked гуртки and revokes the unticked. Dismisses with true
 * once saved.
 */
@Component({
  selector: 'app-kv-mentor-sheet',
  imports: [
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonButton,
    IonContent,
    IonList,
    IonListHeader,
    IonItem,
    IonItemGroup,
    IonLabel,
    IonNote,
    IonIcon,
    IonSpinner,
  ],
  styles: `
    ion-note {
      max-width: 60%;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .mark {
      font-size: 24px;
      color: var(--lk-faint);
    }
    .mark.on {
      color: var(--lk-primary);
    }
    .hint {
      margin: 0 20px;
      font-size: 13px;
      color: var(--lk-muted);
    }
  `,
  template: `
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-button (click)="close(false)">Скасувати</ion-button>
        </ion-buttons>
        <ion-title>Впорядник</ion-title>
        <ion-buttons slot="end">
          <ion-button [strong]="true" [disabled]="!canSave() || busy()" (click)="save()" data-testid="save-mentor">
            @if (busy()) { <ion-spinner name="crescent" /> } @else { Зберегти }
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <ion-list [inset]="true">
        <ion-item-group>
          <ion-item [button]="true" [detail]="true" [disabled]="busy()" (click)="chooseMentor()" data-testid="mentor">
            <ion-label>Впорядник</ion-label>
            <ion-note slot="end" [color]="mentor() ? undefined : 'medium'">{{ mentorName() }}</ion-note>
          </ion-item>
        </ion-item-group>
      </ion-list>

      <ion-list [inset]="true" data-testid="mentor-groups">
        <ion-list-header><ion-label>Гуртки</ion-label></ion-list-header>
        <ion-item-group>
          @for (group of groups; track group.groupKey) {
            <ion-item
              [button]="true"
              [detail]="false"
              [disabled]="busy() || !mentor()"
              (click)="toggle(group.groupKey)"
              [attr.aria-pressed]="isPicked(group.groupKey)"
              data-testid="mentor-group"
            >
              <ion-label>{{ group.name }}</ion-label>
              <ion-icon
                slot="end"
                class="mark"
                [class.on]="isPicked(group.groupKey)"
                [name]="isPicked(group.groupKey) ? 'checkmark-circle' : 'ellipse-outline'"
                aria-hidden="true"
              />
            </ion-item>
          } @empty {
            <ion-item><ion-label>Гуртків поки немає.</ion-label></ion-item>
          }
        </ion-item-group>
      </ion-list>
      <p class="hint">Впорядник матиме доступ до обраних гуртків.</p>
    </ion-content>
  `,
})
export class KvMentorSheet implements OnInit {
  private readonly data = inject(KurinApi);
  private readonly modals = inject(ModalController);
  private readonly toasts = inject(Toasts);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;

  // Set by ModalController's componentProps.
  /** Who may be a впорядник: the kurin's people with an account (`mentor-candidates`). */
  candidates: MemberLookupDto[] = [];
  groups: GroupDto[] = [];
  /** The КВ as shown, to know whose гуртки are whose. */
  rows: KvRow[] = [];
  /** The row this was opened from, or null to add a впорядник. */
  row: KvRow | null = null;

  protected readonly mentor = signal<MemberLookupDto | null>(null);
  protected readonly picked = signal<string[]>([]);
  protected readonly busy = signal(false);
  protected readonly mentorName = computed(() => {
    const mentor = this.mentor();
    return mentor ? fullName(mentor) : 'Оберіть учасника';
  });
  private readonly before = computed(() => this.groupsOf(this.mentor()));
  protected readonly canSave = computed(() => {
    if (!this.mentor()?.userKey) return false;
    const changes = mentorChanges(this.before(), this.picked());
    return changes.assign.length + changes.revoke.length > 0;
  });

  constructor() {
    addIcons({ checkmarkCircle, ellipseOutline });
  }

  ngOnInit(): void {
    const member = this.row?.member ?? null;
    this.mentor.set(member);
    this.picked.set(this.groupsOf(member));
  }

  protected isPicked(groupKey: string): boolean {
    return this.picked().includes(groupKey);
  }

  protected toggle(groupKey: string): void {
    this.picked.update((keys) => (keys.includes(groupKey) ? keys.filter((k) => k !== groupKey) : [...keys, groupKey]));
  }

  protected async chooseMentor(): Promise<void> {
    const people = this.candidates.filter((c) => !!c.userKey);
    const modal = await this.modals.create({
      component: PeoplePickSheet,
      componentProps: {
        title: 'Впорядник',
        people: people.map((member): PickPerson => ({ key: member.userKey!, member })),
        selected: this.mentor()?.userKey ? [this.mentor()!.userKey!] : [],
        emptyText: 'У курені ще нікого з акаунтом немає.',
      },
      presentingElement: this.host.closest('ion-modal') ?? undefined,
    });
    await modal.present();
    const { data } = await modal.onWillDismiss<string[] | null>();
    const picked = people.find((p) => p.userKey === data?.[0]);
    if (!picked) return;
    // Someone else: start from the гуртки they already have.
    this.mentor.set(picked);
    this.picked.set(this.groupsOf(picked));
  }

  protected async save(): Promise<void> {
    const userKey = this.mentor()?.userKey;
    if (!userKey || !this.canSave() || this.busy()) return;
    this.busy.set(true);
    try {
      await this.data.saveMentorGroups(userKey, mentorChanges(this.before(), this.picked()));
      await this.toasts.show('Гуртки впорядника збережено.');
      this.close(true);
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося зберегти гуртки. Спробуй ще раз.'), 'danger');
    } finally {
      this.busy.set(false);
    }
  }

  protected close(saved: boolean): void {
    const own = this.host.closest('ion-modal');
    void (own ? own.dismiss(saved) : this.modals.dismiss(saved));
  }

  private groupsOf(member: MemberLookupDto | null): string[] {
    if (!member) return [];
    const same = (r: KvRow) => (r.member.userKey && member.userKey ? r.member.userKey === member.userKey : r.member.memberKey === member.memberKey);
    return [...(this.rows.find(same)?.groupKeys ?? [])];
  }
}
