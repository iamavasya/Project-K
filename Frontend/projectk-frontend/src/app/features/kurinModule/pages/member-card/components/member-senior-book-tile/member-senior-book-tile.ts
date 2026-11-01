import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from '@openng/optimus-ui/button';
import { DatePickerModule } from '@openng/optimus-ui/datepicker';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { SelectModule } from '@openng/optimus-ui/select';
import { TextareaModule } from '@openng/optimus-ui/textarea';
import { MessageService } from '@openng/optimus-ui/api';
import { EmptyStateComponent } from '../../../../../../shared/empty-state/empty-state';
import { MemberSeniorRecordDto, SeniorCatalogItem, UpsertSeniorRecordRequest } from '../../../../models/senior-record.dto';
import { SeniorRecordService } from '../../../../services/senior-record-service/senior-record.service';
import { parseDateOnlyString, toDateOnlyString } from '../../../../functions/to-date-only-string.function';
import { BOOK_KINDS, BOOK_SECTION_TITLES, BookSection, bookSections, recordLine } from './senior-book.function';

/**
 * Книжечка старшого: ступені, відзначення й вишколи, які людина вносить сама і ніхто не
 * підтверджує. Пише лише власник картки; решта, хто бачить картку, лише читає.
 */
@Component({
  selector: 'app-member-senior-book-tile',
  imports: [FormsModule, ButtonModule, DatePickerModule, DialogModule, InputTextModule, SelectModule, TextareaModule, EmptyStateComponent],
  templateUrl: './member-senior-book-tile.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './member-senior-book-tile.css'
})
export class MemberSeniorBookTileComponent {
  private readonly service = inject(SeniorRecordService);
  private readonly messages = inject(MessageService);

  readonly records = input<MemberSeniorRecordDto[]>([]);
  /** Whether the viewer is the person the card is about. */
  readonly editable = input(false);
  readonly recordsChange = output<MemberSeniorRecordDto[]>();

  readonly catalog = signal<SeniorCatalogItem[]>([]);
  readonly sections = computed<BookSection[]>(() => bookSections(this.records(), this.catalog()));
  readonly line = recordLine;
  readonly today = new Date();

  /** The picker's choices, grouped as the книжечка is. */
  readonly options = computed(() => BOOK_KINDS
    .map(kind => ({ label: BOOK_SECTION_TITLES[kind], items: this.catalog().filter(item => item.kind === kind) }))
    .filter(group => group.items.length > 0));

  dialogVisible = false;
  editing: MemberSeniorRecordDto | null = null;
  catalogId: string | null = null;
  number = '';
  achievedOn: Date | null = null;
  note = '';
  saving = false;

  constructor() {
    this.service.catalog().subscribe({
      next: items => this.catalog.set(items),
      error: () => this.catalog.set([])
    });
  }

  get chosen(): SeniorCatalogItem | undefined {
    return this.catalog().find(item => item.id === this.catalogId);
  }

  open(record: MemberSeniorRecordDto | null): void {
    if (!this.editable()) {
      return;
    }

    this.editing = record;
    this.catalogId = record?.catalogId ?? null;
    this.number = record?.number ?? '';
    this.achievedOn = parseDateOnlyString(record?.achievedOn ?? null);
    this.note = record?.note ?? '';
    this.dialogVisible = true;
  }

  save(): void {
    if (!this.catalogId || this.saving) {
      return;
    }

    const request: UpsertSeniorRecordRequest = {
      catalogId: this.catalogId,
      number: this.chosen?.kind === 'Training' ? this.number.trim() || null : null,
      achievedOn: toDateOnlyString(this.achievedOn),
      note: this.note.trim() || null
    };
    const editing = this.editing;
    this.saving = true;
    const call = editing ? this.service.update(editing.memberSeniorRecordKey, request) : this.service.add(request);
    call.subscribe({
      next: saved => {
        this.saving = false;
        this.dialogVisible = false;
        this.recordsChange.emit(editing
          ? this.records().map(r => r.memberSeniorRecordKey === saved.memberSeniorRecordKey ? saved : r)
          : [saved, ...this.records()]);
      },
      error: error => {
        this.saving = false;
        this.messages.add({ severity: 'error', summary: error?.status === 409 ? 'Цей ступінь уже є' : 'Не збережено' });
      }
    });
  }

  remove(): void {
    const editing = this.editing;
    if (!editing || this.saving) {
      return;
    }

    this.saving = true;
    this.service.remove(editing.memberSeniorRecordKey).subscribe({
      next: () => {
        this.saving = false;
        this.dialogVisible = false;
        this.recordsChange.emit(this.records().filter(r => r.memberSeniorRecordKey !== editing.memberSeniorRecordKey));
      },
      error: () => {
        this.saving = false;
        this.messages.add({ severity: 'error', summary: 'Не видалено' });
      }
    });
  }
}
