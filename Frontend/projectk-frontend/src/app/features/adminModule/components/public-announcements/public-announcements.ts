import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
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
    LocalUtcDatePipe
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './public-announcements.html',
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
    { label: 'All', value: null },
    { label: 'Draft', value: 'Draft' },
    { label: 'Pending', value: 'PendingApproval' },
    { label: 'Approved', value: 'Approved' },
    { label: 'Failed', value: 'Failed' },
    { label: 'Published', value: 'Published' },
    { label: 'Rejected', value: 'Rejected' }
  ];

  readonly parseModeOptions: Option<PublicAnnouncementParseMode>[] = [
    { label: 'Plain text', value: 'PlainText' },
    { label: 'HTML', value: 'Html' },
    { label: 'MarkdownV2', value: 'MarkdownV2' }
  ];

  readonly imagePlacementOptions: Option<PublicAnnouncementImagePlacement>[] = [
    { label: 'Image above text', value: 'ImageFirst' },
    { label: 'Image below text', value: 'ImageLast' }
  ];

  readonly sourceTypeOptions: Option<PublicAnnouncementSourceType>[] = [
    { label: 'Manual', value: 'Manual' },
    { label: 'Backend', value: 'Backend' },
    { label: 'GitHub release', value: 'GitHubRelease' },
    { label: 'GitHub workflow', value: 'GitHubWorkflow' },
    { label: 'Health monitor', value: 'HealthMonitor' }
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
        this.showHttpError('Load failed', error, 'Could not load announcements.');
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
        this.showHttpError('Cleanup status failed', error, 'Could not load cleanup status.');
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
      this.messageService.add({ severity: 'warn', summary: 'Missing content', detail: 'Title and body are required.' });
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
        this.messageService.add({ severity: 'success', summary: 'Saved', detail: 'Announcement draft saved.' });
        this.loadPreview(draft.publicAnnouncementDraftKey);
        this.loadDrafts();
        this.loadCleanupStatus();
      },
      error: error => {
        this.saving = false;
        this.showHttpError('Save failed', error, 'Could not save draft.');
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
      error: error => this.showHttpError('Preview failed', error, 'Could not render Telegram preview.')
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
      this.messageService.add({ severity: 'warn', summary: 'Invalid file', detail: 'Only image files can be uploaded.' });
      return;
    }

    if (file.size > 8 * 1024 * 1024) {
      this.messageService.add({ severity: 'warn', summary: 'Image too large', detail: 'Image must be 8 MB or smaller.' });
      return;
    }

    this.uploadingImage = true;
    this.announcementService.uploadImage(file).subscribe({
      next: result => {
        this.uploadingImage = false;
        this.form.imageUrl = result.imageUrl;
        this.form.imageBlobKey = result.imageBlobKey;
        this.messageService.add({ severity: 'success', summary: 'Uploaded', detail: 'Image URL attached to the draft.' });
        this.loadCleanupStatus();
      },
      error: error => {
        this.uploadingImage = false;
        this.showHttpError('Upload failed', error, 'Could not upload image.');
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
        error: error => this.showHttpError('Image cleanup failed', error, 'Could not delete uploaded image.')
      });
    }
  }

  submit(draft: PublicAnnouncementDraft): void {
    this.runDraftAction('Submit for approval?', 'Submit', () => this.announcementService.submitDraft(draft.publicAnnouncementDraftKey));
  }

  approve(draft: PublicAnnouncementDraft): void {
    this.runDraftAction('Approve this announcement?', 'Approve', () => this.announcementService.approveDraft(draft.publicAnnouncementDraftKey));
  }

  reject(draft: PublicAnnouncementDraft): void {
    this.runDraftAction('Reject this announcement?', 'Reject', () => this.announcementService.rejectDraft(draft.publicAnnouncementDraftKey), 'warn');
  }

  publish(draft: PublicAnnouncementDraft): void {
    this.runDraftAction('Publish this announcement to Telegram now?', 'Publish', () => this.announcementService.publishDraft(draft.publicAnnouncementDraftKey), 'success');
  }

  delete(draft: PublicAnnouncementDraft): void {
    this.runDraftAction('Delete this announcement draft?', 'Delete', () => this.announcementService.deleteDraft(draft.publicAnnouncementDraftKey), 'danger');
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
      return 'Already has Telegram message id';
    }

    return 'Publish';
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
            this.messageService.add({ severity, summary: actionLabel, detail: `Announcement is ${draft.status}.` });
            this.loadDrafts();
            this.loadCleanupStatus();
            if (this.selectedDraft?.publicAnnouncementDraftKey === draft.publicAnnouncementDraftKey) {
              this.selectedDraft = draft;
              this.syncFormFromDraft(draft);
              this.loadPreview(draft.publicAnnouncementDraftKey);
            }
          },
          error: error => {
            this.showHttpError(`${actionLabel} failed`, error, 'Action failed.');
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
