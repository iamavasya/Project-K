import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { HttpResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from '@openng/optimus-ui/table';
import { ButtonModule } from '@openng/optimus-ui/button';
import { MultiSelectModule } from '@openng/optimus-ui/multiselect';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { IconFieldModule } from '@openng/optimus-ui/iconfield';
import { InputIconModule } from '@openng/optimus-ui/inputicon';
import { TagModule } from '@openng/optimus-ui/tag';

import { MemberService } from '../common/services/member-service/member.service';
import { KurinService } from '../common/services/kurin-service/kurin.service';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { MemberDto } from '../common/models/memberDto';
import { KurinBranch, KURIN_BRANCH_LABELS } from '../common/models/enums/kurin-branch.enum';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state';
import { REGISTRY_COLUMNS, RegistryColumn, defaultColumnIdsFor } from './registry-columns';

/** Ключ вибору колонок. На курінь, бо в різних гілках потрібне різне. */
const columnChoiceKey = (kurinKey: string) => `registry:columns:${kurinKey}`;

@Component({
  selector: 'app-registry',
  imports: [
    TableModule,
    ButtonModule,
    MultiSelectModule,
    InputTextModule,
    IconFieldModule,
    InputIconModule,
    TagModule,
    FormsModule,
    DatePipe,
    EmptyStateComponent
  ],
  templateUrl: './registry.html',
  styleUrl: './registry.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegistryComponent implements OnInit {
  private readonly memberService = inject(MemberService);
  private readonly kurinService = inject(KurinService);
  private readonly authService = inject(AuthService);

  readonly members = signal<MemberDto[]>([]);
  readonly loading = signal(true);
  readonly branch = signal<KurinBranch | null>(null);
  readonly kurinNumber = signal<number | null>(null);
  readonly selectedColumnIds = signal<string[]>([]);
  readonly exporting = signal(false);

  readonly allColumns: RegistryColumn[] = REGISTRY_COLUMNS;

  readonly branchLabel = computed(() => {
    const branch = this.branch();
    return branch ? KURIN_BRANCH_LABELS[branch] : null;
  });

  /** Обрані колонки в порядку драбини, а не в порядку кліків. */
  readonly visibleColumns = computed<RegistryColumn[]>(() => {
    const chosen = new Set(this.selectedColumnIds());
    return this.allColumns.filter(column => chosen.has(column.id));
  });

  readonly searchFields = ['lastName', 'firstName', 'middleName', 'email', 'phoneNumber', 'groupName'];

  private kurinKey = '';

  ngOnInit(): void {
    this.authService.getAuthState().subscribe(state => {
      if (!state?.kurinKey) {
        return;
      }

      this.kurinKey = state.kurinKey;
      this.loadKurin();
      this.loadMembers();
    });
  }

  private loadKurin(): void {
    this.kurinService.getByKey(this.kurinKey).subscribe({
      next: kurin => {
        const branch = kurin.branch ?? KurinBranch.UPYu;
        this.branch.set(branch);
        this.kurinNumber.set(kurin.number);
        this.selectedColumnIds.set(this.restoreColumnChoice(branch));
      },
      // A kurin we cannot read still lists its people; the columns just fall back to the youth set.
      error: () => this.selectedColumnIds.set(this.restoreColumnChoice(KurinBranch.UPYu))
    });
  }

  private loadMembers(): void {
    this.loading.set(true);
    this.memberService.getAll(undefined, this.kurinKey).subscribe({
      next: members => {
        this.members.set([...members].sort((left, right) => this.byName(left, right)));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private byName(left: MemberDto, right: MemberDto): number {
    return `${left.lastName} ${left.firstName}`.localeCompare(`${right.lastName} ${right.firstName}`, 'uk');
  }

  fullName(member: MemberDto): string {
    return [member.lastName, member.firstName, member.middleName].filter(Boolean).join(' ');
  }

  cell(member: MemberDto, column: RegistryColumn): string | Date | null {
    return column.value(member);
  }

  asDate(value: string | Date | null): Date | null {
    return value instanceof Date ? value : null;
  }

  onColumnsChange(ids: string[]): void {
    this.selectedColumnIds.set(ids);
    if (this.kurinKey) {
      localStorage.setItem(columnChoiceKey(this.kurinKey), JSON.stringify(ids));
    }
  }

  resetColumns(): void {
    const ids = defaultColumnIdsFor(this.branch());
    this.selectedColumnIds.set(ids);
    if (this.kurinKey) {
      localStorage.removeItem(columnChoiceKey(this.kurinKey));
    }
  }

  /** Вивантажує рівно те, що на екрані — і з тим самим маскуванням, бо файл робить той самий читач. */
  exportToExcel(): void {
    if (!this.kurinKey || this.exporting()) {
      return;
    }

    this.exporting.set(true);
    this.kurinService.exportRegistry(this.kurinKey, this.selectedColumnIds()).subscribe({
      next: response => {
        if (response.body) {
          this.saveBlob(response.body, this.fileNameFrom(response));
        }
        this.exporting.set(false);
      },
      error: () => this.exporting.set(false)
    });
  }

  private fileNameFrom(response: HttpResponse<Blob>): string {
    const disposition = response.headers.get('content-disposition');
    const match = disposition?.match(/filename\*?=(?:UTF-8''|")?([^";]+)/i);
    if (match?.[1]) {
      return decodeURIComponent(match[1].replace(/"$/g, ''));
    }

    return `reyestr-kurin-${this.kurinNumber() ?? ''}.xlsx`;
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    window.URL.revokeObjectURL(url);
  }

  /**
   * Що людина вибрала минулого разу — або пресет гілки. Збережений вибір чиститься від колонок,
   * яких уже немає: інакше видалена колонка жила б у сховищі вічно.
   */
  private restoreColumnChoice(branch: KurinBranch): string[] {
    const stored = localStorage.getItem(columnChoiceKey(this.kurinKey));
    if (!stored) {
      return defaultColumnIdsFor(branch);
    }

    try {
      const parsed: unknown = JSON.parse(stored);
      if (!Array.isArray(parsed)) {
        return defaultColumnIdsFor(branch);
      }

      const known = new Set(this.allColumns.map(column => column.id));
      const kept = parsed.filter((id): id is string => typeof id === 'string' && known.has(id));
      return kept.length ? kept : defaultColumnIdsFor(branch);
    } catch {
      return defaultColumnIdsFor(branch);
    }
  }
}
