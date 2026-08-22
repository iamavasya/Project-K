import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from '@openng/optimus-ui/api';
import { ButtonModule } from '@openng/optimus-ui/button';
import { ConfirmDialogModule } from '@openng/optimus-ui/confirmdialog';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { SelectModule } from '@openng/optimus-ui/select';
import { TableModule } from '@openng/optimus-ui/table';
import { TagModule } from '@openng/optimus-ui/tag';
import { TextareaModule } from '@openng/optimus-ui/textarea';
import { ToastModule } from '@openng/optimus-ui/toast';
import { TooltipModule } from '@openng/optimus-ui/tooltip';
import {
  PublicAnnouncementCleanupStatus,
  PublicAnnouncementDraft,
  PublicAnnouncementDraftRequest,
  PublicAnnouncementImagePlacement,
  PublicAnnouncementParseMode,
  PublicAnnouncementPreview,
  PublicAnnouncementSourceType,
  PublicAnnouncementStatus
} from '../../models/public-announcement.model';
import { PublicAnnouncementService } from '../../services/public-announcement.service';
import { LocalUtcDatePipe } from '../../../../shared/pipes/local-utc-date.pipe';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state';

interface Option<T> {
  label: string;
  value: T;
}

@Component({
  selector: 'app-public-announcements',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    TagModule,
    ToastModule,
    ConfirmDialogModule,
    DialogModule,
    TextareaModule,
    InputTextModule,
    SelectModule,
    TooltipModule,
    LocalUtcDatePipe,
    EmptyStateComponent
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './public-announcements.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './public-announcements.css'
})
export class PublicAnnouncementsComponent implements OnInit {
  private readonly telegramTextLimit = 4096;
  private readonly telegramCaptionLimit = 1024;

  private readonly announcementService = inject(PublicAnnouncementService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  drafts: PublicAnnouncementDraft[] = [];
  selectedDraft: PublicAnnouncementDraft | null = null;
  preview: PublicAnnouncementPreview | null = null;
  cleanupStatus: PublicAnnouncementCleanupStatus | null = null;
  loading = false;
  saving = false;
  uploadingImage = false;
  loadingCleanupStatus = false;
  editorVisible = false;
  selectedStatus: PublicAnnouncementStatus | null = null;

  readonly statusOptions: Option<PublicAnnouncementStatus | null>[] = [
    { label: 'Усі', value: null },
    { label: 'Чернетка', value: 'Draft' },
    { label: 'На розгляді', value: 'PendingApproval' },
    { label: 'Схвалено', value: 'Approved' },
    { label: 'Помилка', value: 'Failed' },
    { label: 'Опубліковано', value: 'Published' },
    { label: 'Відхилено', value: 'Rejected' }
  ];

  readonly parseModeOptions: Option<PublicAnnouncementParseMode>[] = [
    { label: 'Звичайний текст', value: 'PlainText' },
    { label: 'HTML', value: 'Html' },
    { label: 'MarkdownV2', value: 'MarkdownV2' }
  ];

  readonly imagePlacementOptions: Option<PublicAnnouncementImagePlacement>[] = [
    { label: 'Зображення над текстом', value: 'ImageFirst' },
    { label: 'Зображення під текстом', value: 'ImageLast' }
  ];

  readonly sourceTypeOptions: Option<PublicAnnouncementSourceType>[] = [
    { label: 'Вручну', value: 'Manual' },
    { label: 'Backend', value: 'Backend' },
    { label: 'Реліз GitHub', value: 'GitHubRelease' },
    { label: 'Workflow GitHub', value: 'GitHubWorkflow' },
    { label: 'Моніторинг стану', value: 'HealthMonitor' }
  ];

  form: PublicAnnouncementDraftRequest = this.createEmptyForm();

  ngOnInit(): void {
    this.loadDrafts();
    this.loadCleanupStatus();
  }

  loadDrafts(): void {
    this.loading = true;
    this.announcementService.getDrafts(this.selectedStatus).subscribe({
      next: drafts => {
        this.drafts = drafts ?? [];
        this.loading = false;
      },
      error: error => {
        this.loading = false;
        this.showHttpError('Не вдалося завантажити', error, 'Оголошення не завантажились.');
      }
    });
  }

  loadCleanupStatus(): void {
    this.loadingCleanupStatus = true;
    this.announcementService.getCleanupStatus().subscribe({
      next: status => {
        this.cleanupStatus = status;
        this.loadingCleanupStatus = false;
      },
      error: error => {
        this.loadingCleanupStatus = false;
        this.showHttpError('Не вдалося завантажити', error, 'Стан прибирання не завантажився.');
      }
    });
  }

  newDraft(): void {
    this.selectedDraft = null;
    this.preview = null;
    this.form = this.createEmptyForm();
    this.editorVisible = true;
  }

  editDraft(draft: PublicAnnouncementDraft): void {
    this.selectedDraft = draft;
    this.preview = null;
    this.form = {
      title: draft.title,
      body: draft.body,
      sourceType: draft.sourceType,
      sourceId: draft.sourceId,
      sourceUrl: draft.sourceUrl,
      environment: draft.environment,
      version: draft.version,
      codename: draft.codename,
      parseMode: draft.parseMode,
      imageBlobKey: draft.imageBlobKey,
      imageUrl: draft.imageUrl,
      imageAltText: draft.imageAltText,
      imagePlacement: draft.imagePlacement ?? 'ImageFirst',
      templateKey: draft.templateKey,
      templateDataJson: draft.templateDataJson
    };
    this.editorVisible = true;
    this.loadPreview(draft.publicAnnouncementDraftKey);
  }

  saveDraft(): void {
    if (!this.form.title.trim() || !this.form.body.trim()) {
      this.messageService.add({ severity: 'warn', summary: 'Бракує вмісту', detail: 'Заголовок і текст обовʼязкові.' });
      return;
    }

    this.saving = true;
    const request = this.cleanRequest(this.form);
    const action = this.selectedDraft
      ? this.announcementService.updateDraft(this.selectedDraft.publicAnnouncementDraftKey, request)
      : this.announcementService.createDraft(request);

    action.subscribe({
      next: draft => {
        this.selectedDraft = draft;
        this.saving = false;
        this.editorVisible = true;
        this.messageService.add({ severity: 'success', summary: 'Збережено', detail: 'Чернетку збережено.' });
        this.loadPreview(draft.publicAnnouncementDraftKey);
        this.loadDrafts();
        this.loadCleanupStatus();
      },
      error: error => {
        this.saving = false;
        this.showHttpError('Не вдалося зберегти', error, 'Чернетку не збережено.');
      }
    });
  }

  loadPreview(draftKey = this.selectedDraft?.publicAnnouncementDraftKey): void {
    if (!draftKey) {
      this.preview = null;
      return;
    }

    this.announcementService.previewDraft(draftKey).subscribe({
      next: preview => this.preview = preview,
      error: error => this.showHttpError('Не вдалося показати', error, 'Перегляд у Telegram не сформувався.')
    });
  }

  uploadImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';

    if (!file) {
      return;
    }

    if (!file.type.startsWith('image/')) {
      this.messageService.add({ severity: 'warn', summary: 'Непідтримуваний файл', detail: 'Завантажити можна лише зображення.' });
      return;
    }

    if (file.size > 8 * 1024 * 1024) {
      this.messageService.add({ severity: 'warn', summary: 'Завелике зображення', detail: 'Розмір має бути до 8 МБ.' });
      return;
    }

    this.uploadingImage = true;
    this.announcementService.uploadImage(file).subscribe({
      next: result => {
        this.uploadingImage = false;
        this.form.imageUrl = result.imageUrl;
        this.form.imageBlobKey = result.imageBlobKey;
        this.messageService.add({ severity: 'success', summary: 'Завантажено', detail: 'Посилання на зображення додано до чернетки.' });
        this.loadCleanupStatus();
      },
      error: error => {
        this.uploadingImage = false;
        this.showHttpError('Не вдалося завантажити', error, 'Зображення не завантажилось.');
      }
    });
  }

  removeImage(): void {
    const imageKey = this.form.imageBlobKey;
    this.form.imageUrl = null;
    this.form.imageBlobKey = null;

    if (imageKey) {
      this.announcementService.deleteImage(imageKey).subscribe({
        next: () => this.loadCleanupStatus(),
        error: error => this.showHttpError('Не вдалося прибрати', error, 'Завантажене зображення не видалилось.')
      });
    }
  }

  submit(draft: PublicAnnouncementDraft): void {
    this.runDraftAction('Подати на розгляд?', 'Подати', () => this.announcementService.submitDraft(draft.publicAnnouncementDraftKey));
  }

  approve(draft: PublicAnnouncementDraft): void {
    this.runDraftAction('Схвалити це оголошення?', 'Схвалити', () => this.announcementService.approveDraft(draft.publicAnnouncementDraftKey));
  }

  reject(draft: PublicAnnouncementDraft): void {
    this.runDraftAction('Відхилити це оголошення?', 'Відхилити', () => this.announcementService.rejectDraft(draft.publicAnnouncementDraftKey), 'warn');
  }

  publish(draft: PublicAnnouncementDraft): void {
    this.runDraftAction('Опублікувати оголошення в Telegram зараз?', 'Опублікувати', () => this.announcementService.publishDraft(draft.publicAnnouncementDraftKey), 'success');
  }

  delete(draft: PublicAnnouncementDraft): void {
    this.runDraftAction('Видалити цю чернетку?', 'Видалити', () => this.announcementService.deleteDraft(draft.publicAnnouncementDraftKey), 'danger');
  }

  canSubmit(draft: PublicAnnouncementDraft): boolean {
    return draft.status === 'Draft' || draft.status === 'Rejected' || draft.status === 'Failed';
  }

  canApprove(draft: PublicAnnouncementDraft): boolean {
    return draft.status === 'Draft' || draft.status === 'PendingApproval';
  }

  canPublish(draft: PublicAnnouncementDraft): boolean {
    return draft.status === 'Approved' && !draft.telegramMessageId;
  }

  getPublishTooltip(draft: PublicAnnouncementDraft): string {
    if (draft.telegramMessageId) {
      return 'Уже має id повідомлення в Telegram';
    }

    return 'Опублікувати';
  }

  hasUploadedImage(): boolean {
    return !!this.form.imageBlobKey;
  }

  getPlacementLabel(placement?: PublicAnnouncementImagePlacement | null): string {
    return placement === 'ImageLast' ? 'Image below text' : 'Image above text';
  }

  selectedPlacementUsesCaptionAboveMedia(): boolean {
    return !!this.form.imageUrl && this.form.imagePlacement === 'ImageLast';
  }

  getRenderedTextEstimate(): string {
    const title = this.form.title.trim();
    const body = this.form.body.trim().replace(/\r\n/g, '\n');

    if (!title && !body) {
      return '';
    }

    if (this.form.parseMode === 'Html') {
      return `<b>${title}</b>\n\n${body}`;
    }

    if (this.form.parseMode === 'MarkdownV2') {
      return `*${title}*\n\n${body}`;
    }

    return `${title}\n\n${body}`;
  }

  getRenderedTextLength(): number {
    return this.getRenderedTextEstimate().length;
  }

  exceedsTelegramMessageLimit(): boolean {
    return this.getRenderedTextLength() > this.telegramTextLimit;
  }

  exceedsTelegramCaptionLimit(): boolean {
    return !!this.form.imageUrl && this.getRenderedTextLength() > this.telegramCaptionLimit;
  }

  getMessageLimitLabel(): string {
    return `${this.getRenderedTextLength()}/${this.telegramTextLimit}`;
  }

  getCaptionLimitLabel(): string {
    return `${this.getRenderedTextLength()}/${this.telegramCaptionLimit}`;
  }

  canReject(draft: PublicAnnouncementDraft): boolean {
    return draft.status === 'Draft' || draft.status === 'PendingApproval' || draft.status === 'Approved' || draft.status === 'Failed';
  }

  getStatusSeverity(status: PublicAnnouncementStatus): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    switch (status) {
      case 'Published':
        return 'success';
      case 'Approved':
        return 'info';
      case 'PendingApproval':
        return 'warn';
      case 'Failed':
      case 'Rejected':
        return 'danger';
      case 'Deleted':
        return 'secondary';
      default:
        return 'contrast';
    }
  }

  private runDraftAction(
    message: string,
    actionLabel: string,
    action: () => ReturnType<PublicAnnouncementService['approveDraft']>,
    severity: 'success' | 'info' | 'warn' | 'danger' = 'info'
  ): void {
    this.confirmationService.confirm({
      message,
      header: actionLabel,
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        action().subscribe({
          next: draft => {
            this.messageService.add({ severity, summary: actionLabel, detail: `Статус оголошення: ${draft.status}.` });
            this.loadDrafts();
            this.loadCleanupStatus();
            if (this.selectedDraft?.publicAnnouncementDraftKey === draft.publicAnnouncementDraftKey) {
              this.selectedDraft = draft;
              this.syncFormFromDraft(draft);
              this.loadPreview(draft.publicAnnouncementDraftKey);
            }
          },
          error: error => {
            this.showHttpError(`Не вдалося: ${actionLabel.toLowerCase()}`, error, 'Дію не виконано.');
          }
        });
      }
    });
  }

  private createEmptyForm(): PublicAnnouncementDraftRequest {
    return {
      title: '',
      body: '',
      sourceType: 'Manual',
      environment: 'sandbox',
      version: '',
      codename: '',
      parseMode: 'Html',
      imagePlacement: 'ImageFirst',
      imageUrl: ''
    };
  }

  private syncFormFromDraft(draft: PublicAnnouncementDraft): void {
    this.form = {
      title: draft.title,
      body: draft.body,
      sourceType: draft.sourceType,
      sourceId: draft.sourceId,
      sourceUrl: draft.sourceUrl,
      environment: draft.environment,
      version: draft.version,
      codename: draft.codename,
      parseMode: draft.parseMode,
      imageBlobKey: draft.imageBlobKey,
      imageUrl: draft.imageUrl,
      imageAltText: draft.imageAltText,
      imagePlacement: draft.imagePlacement ?? 'ImageFirst',
      templateKey: draft.templateKey,
      templateDataJson: draft.templateDataJson
    };
  }

  private cleanRequest(request: PublicAnnouncementDraftRequest): PublicAnnouncementDraftRequest {
    return {
      ...request,
      title: request.title.trim(),
      body: request.body.trim(),
      sourceId: this.emptyToNull(request.sourceId),
      sourceUrl: this.emptyToNull(request.sourceUrl),
      environment: this.emptyToNull(request.environment),
      version: this.emptyToNull(request.version),
      codename: this.emptyToNull(request.codename),
      imageBlobKey: this.emptyToNull(request.imageBlobKey),
      imageUrl: this.emptyToNull(request.imageUrl),
      imageAltText: this.emptyToNull(request.imageAltText),
      imagePlacement: request.imagePlacement ?? 'ImageFirst',
      templateKey: this.emptyToNull(request.templateKey),
      templateDataJson: this.emptyToNull(request.templateDataJson)
    };
  }

  private emptyToNull(value?: string | null): string | null {
    const normalized = value?.trim();
    return normalized ? normalized : null;
  }

  private showHttpError(summary: string, error: unknown, fallback: string): void {
    const detail = this.resolveHttpErrorDetail(error, fallback);

    this.messageService.add({ severity: 'error', summary, detail });
  }

  private resolveHttpErrorDetail(error: unknown, fallback: string): string {
    if (!error || typeof error !== 'object') {
      return fallback;
    }

    const payload = error as {
      error?: { message?: string; error?: string };
      message?: string;
    };

    return payload.error?.message
      ?? payload.error?.error
      ?? payload.message
      ?? fallback;
  }
}
