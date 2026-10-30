import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ActionSheetController,
  IonBackButton,
  IonBadge,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonInput,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonListHeader,
  IonNote,
  IonRefresher,
  IonRefresherContent,
  IonSegment,
  IonSegmentButton,
  IonSelect,
  IonSelectOption,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';
import { AuthService } from '../../auth/auth.service';
import { apiErrorText } from '../../core/api';
import { FAILED_TEXT, Loaded, settle, valueOf } from '../../core/loaded';
import { Toasts } from '../../core/toast';
import { MembershipCandidateDto, MembershipRequestDto, MembershipRequestKind, MembershipRequestKurinDto, MembershipRequestStatus } from './kurin.models';
import { KurinApi } from './kurin.service';

const STATUS: Record<MembershipRequestStatus, string> = {
  Pending: 'чекає',
  Approved: 'підтверджено',
  Declined: 'відхилено',
  Cancelled: 'скасовано',
  Expired: 'минув строк',
};

/** The API's request error codes, in the web's words (web transfers ERRORS). */
const ERRORS: Record<string, string> = {
  AlreadyAMember: 'Ця людина вже у вашому курені.',
  RequestPending: 'Запит щодо цієї людини вже чекає відповіді.',
  ChooseKurin: 'Людина стоїть у кількох куренях — вибери, кого питати.',
  NotInThatKurin: 'Людина вже не стоїть у тому курені. Знайди її за кодом ще раз.',
  MemberOwesDues: 'Людина винна куреню вкладку за квартали, що минули. Перевести можна, коли її сплачено повністю.',
  RequestClosed: 'На цей запит уже відповіли, його скасовано або минув строк.',
  NoSuchCode: 'Такого коду немає. Перевір його з людиною.',
};

const kurinNo = (k: MembershipRequestKurinDto): string => (k.number != null ? `к. ч. ${k.number}` : 'підготовчий курінь');

/**
 * «Переходи» on a phone (web pages/transfers): the Звʼязковий asks for a person by the code they
 * gave, and answers other kurins' requests. A tap on a request opens what can be done with it.
 */
@Component({
  selector: 'app-transfers',
  imports: [
    FormsModule,
    IonHeader,
    IonToolbar,
    IonButtons,
    IonBackButton,
    IonTitle,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    IonSegment,
    IonSegmentButton,
    IonList,
    IonListHeader,
    IonItemGroup,
    IonItem,
    IonLabel,
    IonNote,
    IonBadge,
    IonInput,
    IonButton,
    IonSelect,
    IonSelectOption,
  ],
  styles: `
    .owes {
      color: var(--ion-color-danger);
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/kurin" text="" /></ion-buttons>
        <ion-title>Переходи</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" (ionRefresh)="refresh($event)"><ion-refresher-content /></ion-refresher>

      <ion-list [inset]="true" data-testid="ask">
        <ion-list-header><ion-label>Попросити людину</ion-label></ion-list-header>
        <ion-item-group>
          <ion-item>
            <ion-input
              label="Код людини"
              labelPlacement="stacked"
              placeholder="PL-XXXXX-XXXXX"
              autocapitalize="characters"
              [(ngModel)]="code"
              data-testid="code"
            />
          </ion-item>
          <ion-item [button]="true" [detail]="false" [disabled]="!code.trim() || busy()" (click)="find()" data-testid="find">
            <ion-label color="primary">Знайти</ion-label>
          </ion-item>
          @if (candidate(); as c) {
            <ion-item>
              <ion-label class="ion-text-wrap">
                <h2>{{ c.member.fullName }}</h2>
                <p>{{ whereNow(c) }}</p>
              </ion-label>
            </ion-item>
            @if (!c.alreadyInThisKurin) {
              @if (c.currentKurins.length) {
                <ion-item>
                  <ion-select label="Що просимо" interface="action-sheet" cancelText="Скасувати" [(ngModel)]="kind">
                    <ion-select-option value="Transfer">Перевести до нас</ion-select-option>
                    <ion-select-option value="AlsoJoin">Прийняти ще й до нас</ion-select-option>
                  </ion-select>
                </ion-item>
                @if (c.currentKurins.length > 1) {
                  <ion-item>
                    <ion-select label="Кого питати" interface="action-sheet" cancelText="Скасувати" [(ngModel)]="fromKurinKey">
                      @for (k of c.currentKurins; track k.kurinKey) {
                        <ion-select-option [value]="k.kurinKey">{{ kurinNo(k) }}</ion-select-option>
                      }
                    </ion-select>
                  </ion-item>
                }
              }
              <ion-item>
                <ion-select label="У вашому курені як" interface="action-sheet" cancelText="Скасувати" [(ngModel)]="membershipKind">
                  <ion-select-option value="Youth">Юнак</ion-select-option>
                  <ion-select-option value="Staff">Впорядник</ion-select-option>
                </ion-select>
              </ion-item>
              <ion-item [button]="true" [detail]="false" [disabled]="busy() || (c.currentKurins.length > 1 && !fromKurinKey)" (click)="send(c)" data-testid="send">
                <ion-label color="primary">{{ c.currentKurins.length ? 'Надіслати запит' : 'Прийняти до куреня' }}</ion-label>
              </ion-item>
            }
          }
        </ion-item-group>
      </ion-list>

      <ion-segment [value]="part()" (ionChange)="part.set($any($event).detail.value)">
        <ion-segment-button value="incoming"><ion-label>Чекають відповіді</ion-label></ion-segment-button>
        <ion-segment-button value="outgoing"><ion-label>Надіслані</ion-label></ion-segment-button>
      </ion-segment>

      @if (requests().state === 'failed') {
        <ion-list [inset]="true"><ion-item><ion-label>{{ failed }}</ion-label></ion-item></ion-list>
      } @else {
        <ion-list [inset]="true" [attr.data-testid]="part()">
          <ion-item-group>
            @for (r of shown(); track r.membershipRequestKey) {
              <ion-item [button]="r.status === 'Pending'" [detail]="false" (click)="act(r)" data-testid="request">
                <ion-label class="ion-text-wrap">
                  <h2>{{ r.memberName }}</h2>
                  <p>{{ line(r) }}</p>
                  @if (part() === 'incoming' && r.status === 'Pending' && r.owedToLeave > 0) {
                    <p class="owes">Винна куреню вкладку — перевести можна після сплати.</p>
                  }
                </ion-label>
                <ion-badge slot="end" [class]="r.status === 'Approved' ? 'lk-tag--success' : 'lk-tag--secondary'">{{ status[r.status] }}</ion-badge>
              </ion-item>
            } @empty {
              <ion-item>
                <ion-label class="ion-text-wrap">
                  <p>{{ part() === 'incoming' ? 'Запитів від інших куренів немає.' : 'Ви ще нікого не просили.' }}</p>
                </ion-label>
              </ion-item>
            }
          </ion-item-group>
        </ion-list>
      }
    </ion-content>
  `,
})
export class TransfersPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly data = inject(KurinApi);
  private readonly sheets = inject(ActionSheetController);
  private readonly toasts = inject(Toasts);

  protected readonly failed = FAILED_TEXT;
  protected readonly status = STATUS;
  protected readonly kurinNo = kurinNo;
  protected readonly part = signal<'incoming' | 'outgoing'>('incoming');
  protected readonly requests = signal<Loaded<{ incoming: MembershipRequestDto[]; outgoing: MembershipRequestDto[] }>>({ state: 'loading' });
  protected readonly shown = computed(() => {
    const all = valueOf(this.requests());
    const list = (this.part() === 'incoming' ? all?.incoming : all?.outgoing) ?? [];
    return [...list].sort((a, b) => Number(b.status === 'Pending') - Number(a.status === 'Pending'));
  });
  protected readonly candidate = signal<MembershipCandidateDto | null>(null);
  protected readonly busy = signal(false);

  protected code = '';
  protected kind: MembershipRequestKind = 'Transfer';
  protected membershipKind: 'Youth' | 'Staff' = 'Youth';
  protected fromKurinKey: string | null = null;

  private readonly kurinKey = this.auth.user()?.kurinKey ?? '';

  ngOnInit(): void {
    void this.load();
  }

  protected async refresh(event: Event): Promise<void> {
    await this.load();
    await (event.target as HTMLIonRefresherElement).complete();
  }

  protected whereNow(c: MembershipCandidateDto): string {
    if (c.alreadyInThisKurin) return 'Уже у вашому курені.';
    if (!c.currentKurins.length) return 'Ніде не стоїть — приймеш одразу.';
    return `Зараз у ${c.currentKurins.map(kurinNo).join(', ')}`;
  }

  protected line(r: MembershipRequestDto): string {
    if (this.part() === 'incoming') {
      return r.kind === 'Transfer' ? `${kurinNo(r.toKurin)} просить перевести до себе` : `${kurinNo(r.toKurin)} просить прийняти ще й до себе`;
    }
    return r.kind === 'Transfer' ? `Перевести з ${kurinNo(r.fromKurin)}` : `Ще й до нас, лишається в ${kurinNo(r.fromKurin)}`;
  }

  protected async find(): Promise<void> {
    this.busy.set(true);
    this.candidate.set(null);
    try {
      this.candidate.set(await this.data.membershipCandidate(this.kurinKey, this.code.trim().toUpperCase()));
      this.kind = 'Transfer';
      this.membershipKind = 'Youth';
      this.fromKurinKey = null;
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося знайти. Спробуй ще раз.', ERRORS), 'danger');
    } finally {
      this.busy.set(false);
    }
  }

  protected async send(c: MembershipCandidateDto): Promise<void> {
    this.busy.set(true);
    const asked = c.currentKurins.length === 1 ? c.currentKurins[0].kurinKey : this.fromKurinKey;
    try {
      const sent = await this.data.requestMembership(this.kurinKey, {
        publicId: c.member.publicId,
        kind: this.kind,
        membershipKind: this.membershipKind,
        fromKurinKey: asked,
      });
      await this.toasts.show(sent.joined ? `${c.member.fullName} тепер у вашому курені.` : 'Запит надіслано. Він діє 14 днів.');
      this.candidate.set(null);
      this.code = '';
      this.part.set('outgoing');
      await this.load();
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося надіслати. Спробуй ще раз.', ERRORS), 'danger');
    } finally {
      this.busy.set(false);
    }
  }

  protected async act(r: MembershipRequestDto): Promise<void> {
    if (r.status !== 'Pending') return;
    const incoming = this.part() === 'incoming';
    const owes = incoming && r.kind === 'Transfer' && r.owedToLeave > 0;
    const sheet = await this.sheets.create({
      header: r.memberName,
      subHeader: this.line(r),
      buttons: incoming
        ? [
            ...(owes ? [] : [{ text: r.kind === 'Transfer' ? 'Перевести' : 'Підтвердити', role: r.kind === 'Transfer' ? 'destructive' : undefined, data: 'approve' }]),
            { text: 'Відхилити', role: 'destructive', data: 'decline' },
            { text: 'Закрити', role: 'cancel' },
          ]
        : [
            { text: 'Скасувати запит', role: 'destructive', data: 'cancel' },
            { text: 'Закрити', role: 'cancel' },
          ],
    });
    await sheet.present();
    const { data } = await sheet.onWillDismiss();
    if (!data) return;
    try {
      if (data === 'cancel') await this.data.cancelMembershipRequest(this.kurinKey, r.membershipRequestKey);
      else await this.data.decideMembershipRequest(this.kurinKey, r.membershipRequestKey, data === 'approve');
      await this.toasts.show(data === 'approve' ? 'Підтверджено.' : data === 'decline' ? 'Відхилено.' : 'Запит скасовано.');
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося. Спробуй ще раз.', ERRORS), 'danger');
    }
    await this.load();
  }

  private async load(): Promise<void> {
    if (!this.kurinKey) return;
    await settle(this.data.membershipRequests(this.kurinKey), this.requests);
  }
}
