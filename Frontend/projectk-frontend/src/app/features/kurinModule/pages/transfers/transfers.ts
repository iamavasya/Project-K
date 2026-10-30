import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from '@openng/optimus-ui/button';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { SelectModule } from '@openng/optimus-ui/select';
import { SelectButtonModule } from '@openng/optimus-ui/selectbutton';
import { TableModule } from '@openng/optimus-ui/table';
import { TagModule } from '@openng/optimus-ui/tag';
import { ConfirmDialogModule } from '@openng/optimus-ui/confirmdialog';
import { ConfirmationService, MessageService } from '@openng/optimus-ui/api';
import { AuthService } from '../../../authModule/services/auth-service/auth.service';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state';
import { failureDetail } from '../../../../shared/functions/failure-detail.function';
import { kurinLabel } from '../../../../shared/functions/kurin-label.function';
import {
  MembershipCandidateDto,
  MembershipKindName,
  MembershipRequestDto,
  MembershipRequestKind,
  MembershipRequestKurinDto,
  MembershipRequestStatus,
  MembershipService
} from '../../services/membership-service/membership.service';

type TagSeverity = 'success' | 'secondary' | 'warn' | 'danger';

const STATUS: Record<MembershipRequestStatus, { label: string; severity: TagSeverity }> = {
  Pending: { label: 'Чекає', severity: 'warn' },
  Approved: { label: 'Підтверджено', severity: 'success' },
  Declined: { label: 'Відхилено', severity: 'secondary' },
  Cancelled: { label: 'Скасовано', severity: 'secondary' },
  Expired: { label: 'Минув строк', severity: 'secondary' }
};

/** Що сторінка каже на коди помилок API. */
const ERRORS: Record<string, string> = {
  AlreadyAMember: 'Ця людина вже у вашому курені.',
  RequestPending: 'Запит щодо цієї людини вже чекає відповіді.',
  ChooseKurin: 'Людина стоїть у кількох куренях — вибери, кого питати.',
  NotInThatKurin: 'Людина вже не стоїть у тому курені. Знайди її за кодом ще раз.',
  MemberOwesDues: 'Людина винна куреню вкладку за квартали, що минули. Перевести можна, коли її сплачено повністю.',
  RequestClosed: 'На цей запит уже відповіли, його скасовано або минув строк.'
};

/**
 * «Переходи»: запити між куренями. Звʼязковий куреня Б просить людину — за кодом, який вона дала, —
 * перевести до себе або прийняти ще й до них; Звʼязковий куреня А підтверджує чи відхиляє. Людина,
 * яка ніде не стоїть, приймається одразу — питати нікого.
 */
@Component({
  selector: 'app-transfers',
  imports: [
    DatePipe, FormsModule, ButtonModule, InputTextModule, SelectModule, SelectButtonModule, TableModule, TagModule,
    ConfirmDialogModule, EmptyStateComponent
  ],
  templateUrl: './transfers.html',
  styleUrl: './transfers.css',
  providers: [ConfirmationService],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransfersComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly memberships = inject(MembershipService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  readonly kurinLabel = kurinLabel;
  readonly kindOptions: { label: string; value: MembershipRequestKind }[] = [
    { label: 'Перевести до нас', value: 'Transfer' },
    { label: 'Прийняти ще й до нас', value: 'AlsoJoin' }
  ];
  readonly membershipKindOptions: { label: string; value: MembershipKindName }[] = [
    { label: 'Юнак', value: 'Youth' },
    { label: 'Впорядник', value: 'Staff' }
  ];

  readonly loading = signal(true);
  readonly incoming = signal<MembershipRequestDto[]>([]);
  readonly outgoing = signal<MembershipRequestDto[]>([]);
  readonly pendingIncoming = computed(() => this.incoming().filter(r => r.status === 'Pending').length);

  readonly code = signal('');
  readonly looking = signal(false);
  readonly lookupError = signal<string | null>(null);
  readonly candidate = signal<MembershipCandidateDto | null>(null);
  readonly kind = signal<MembershipRequestKind>('Transfer');
  readonly membershipKind = signal<MembershipKindName>('Youth');
  readonly fromKurinKey = signal<string | null>(null);
  readonly sending = signal(false);

  /** Людина ніде не стоїть: питати нікого, приймаємо одразу. */
  readonly standsNowhere = computed(() => (this.candidate()?.currentKurins.length ?? 0) === 0);
  readonly askedKurin = computed<MembershipRequestKurinDto | null>(() => {
    const kurins = this.candidate()?.currentKurins ?? [];
    return kurins.length === 1 ? kurins[0] : kurins.find(k => k.kurinKey === this.fromKurinKey()) ?? null;
  });
  readonly kurinOptions = computed(() =>
    (this.candidate()?.currentKurins ?? []).map(k => ({ label: this.kurinName(k), value: k.kurinKey })));
  readonly canSend = computed(() => {
    const candidate = this.candidate();
    return !!candidate && !candidate.alreadyInThisKurin && (this.standsNowhere() || !!this.askedKurin());
  });

  private kurinKey = '';

  ngOnInit(): void {
    this.auth.getAuthState().subscribe(state => {
      if (!state?.kurinKey || state.kurinKey === this.kurinKey) {
        return;
      }

      this.kurinKey = state.kurinKey;
      this.load();
    });
  }

  statusLabel(request: MembershipRequestDto): string {
    return STATUS[request.status].label;
  }

  statusSeverity(request: MembershipRequestDto): TagSeverity {
    return STATUS[request.status].severity;
  }

  kurinName(kurin: MembershipRequestKurinDto): string {
    return kurin.namedAfter ? `${kurinLabel(kurin.number)} · ${kurin.namedAfter}` : kurinLabel(kurin.number);
  }

  kindLine(request: MembershipRequestDto, incoming: boolean): string {
    if (incoming) {
      return request.kind === 'Transfer'
        ? `${kurinLabel(request.toKurin.number)} просить перевести до себе`
        : `${kurinLabel(request.toKurin.number)} просить прийняти ще й до себе`;
    }

    return request.kind === 'Transfer'
      ? `Перевести з ${kurinLabel(request.fromKurin.number)}`
      : `Прийняти ще й до нас (лишається в ${kurinLabel(request.fromKurin.number)})`;
  }

  find(): void {
    const code = this.code().trim().toUpperCase();
    if (!code) {
      return;
    }

    this.looking.set(true);
    this.lookupError.set(null);
    this.candidate.set(null);
    this.memberships.candidate(this.kurinKey, code).subscribe({
      next: candidate => {
        this.looking.set(false);
        this.candidate.set(candidate);
        this.kind.set('Transfer');
        this.membershipKind.set('Youth');
        this.fromKurinKey.set(null);
      },
      error: (error: unknown) => {
        this.looking.set(false);
        this.lookupError.set(error instanceof HttpErrorResponse && error.status === 404
          ? 'Такого коду немає. Перевір його з людиною.'
          : failureDetail(error));
      }
    });
  }

  send(): void {
    const candidate = this.candidate();
    if (!candidate || !this.canSend()) {
      return;
    }

    const asked = this.askedKurin();
    this.sending.set(true);
    this.memberships.createRequest(this.kurinKey, {
      publicId: candidate.member.publicId,
      kind: this.kind(),
      membershipKind: this.membershipKind(),
      fromKurinKey: asked?.kurinKey ?? null
    }).subscribe({
      next: sent => {
        this.sending.set(false);
        this.messages.add(sent.joined
          ? { severity: 'success', summary: 'Людину прийнято', detail: `${candidate.member.fullName} тепер у вашому курені.` }
          : { severity: 'success', summary: 'Запит надіслано', detail: `Відповідь дасть Звʼязковий ${kurinLabel(asked?.number ?? null)}. Запит діє 14 днів.` });
        this.candidate.set(null);
        this.code.set('');
        this.load();
      },
      error: (error: unknown) => {
        this.sending.set(false);
        this.messages.add({ severity: 'error', summary: 'Не вдалося надіслати', detail: this.errorText(error) });
      }
    });
  }

  confirmApprove(request: MembershipRequestDto): void {
    const transfer = request.kind === 'Transfer';
    this.confirmation.confirm({
      header: transfer ? 'Перевести людину?' : 'Прийняти ще й туди?',
      message: transfer
        ? `${request.memberName} вийде з вашого куреня й стане членом ${kurinLabel(request.toKurin.number)}. `
          + 'Проби, вмілості, відзначення й перестороги йдуть із людиною.'
        : `${request.memberName} лишається у вашому курені й стане ще й членом ${kurinLabel(request.toKurin.number)}.`,
      icon: transfer ? 'pi pi-sign-out' : 'pi pi-user-plus',
      acceptButtonProps: transfer ? { label: 'Перевести', severity: 'danger' } : { label: 'Підтвердити' },
      rejectButtonProps: { label: 'Скасувати', severity: 'secondary', outlined: true },
      accept: () => this.memberships.approveRequest(this.kurinKey, request.membershipRequestKey).subscribe({
        next: () => {
          this.messages.add({ severity: 'success', summary: 'Підтверджено', detail: `${request.memberName}: запит підтверджено.` });
          this.load();
        },
        error: (error: unknown) => {
          this.messages.add({ severity: 'error', summary: 'Не вдалося підтвердити', detail: this.errorText(error) });
          this.load();
        }
      })
    });
  }

  confirmDecline(request: MembershipRequestDto): void {
    this.confirmation.confirm({
      header: 'Відхилити запит?',
      message: `${kurinLabel(request.toKurin.number)} дізнається, що ви відхилили запит. ${request.memberName} лишається у вашому курені.`,
      icon: 'pi pi-times-circle',
      acceptButtonProps: { label: 'Відхилити', severity: 'danger' },
      rejectButtonProps: { label: 'Скасувати', severity: 'secondary', outlined: true },
      accept: () => this.memberships.declineRequest(this.kurinKey, request.membershipRequestKey).subscribe(() => this.load())
    });
  }

  confirmCancel(request: MembershipRequestDto): void {
    this.confirmation.confirm({
      header: 'Скасувати запит?',
      message: `Запит до ${kurinLabel(request.fromKurin.number)} (${request.memberName}) більше не чекатиме відповіді.`,
      icon: 'pi pi-times-circle',
      acceptButtonProps: { label: 'Скасувати запит', severity: 'danger' },
      rejectButtonProps: { label: 'Лишити', severity: 'secondary', outlined: true },
      accept: () => this.memberships.cancelRequest(this.kurinKey, request.membershipRequestKey).subscribe(() => this.load())
    });
  }

  private load(): void {
    this.loading.set(true);
    this.memberships.requests(this.kurinKey).subscribe({
      next: requests => {
        const pendingFirst = (list: MembershipRequestDto[]) =>
          [...list].sort((a, b) => Number(b.status === 'Pending') - Number(a.status === 'Pending'));
        this.incoming.set(pendingFirst(requests.incoming));
        this.outgoing.set(pendingFirst(requests.outgoing));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private errorText(error: unknown): string {
    const code = error instanceof HttpErrorResponse ? (error.error as { error?: string } | null)?.error : undefined;
    return (code && ERRORS[code]) || failureDetail(error);
  }
}
