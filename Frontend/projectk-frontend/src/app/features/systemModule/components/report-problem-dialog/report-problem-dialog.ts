import { ChangeDetectionStrategy, Component, inject, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { EditorModule } from '@openng/optimus-ui/editor';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { MessageModule } from '@openng/optimus-ui/message';
import { TextareaModule } from '@openng/optimus-ui/textarea';
import { environment } from '../../../../../environments/environment';
import { failureDetail } from '../../../../shared/functions/failure-detail.function';
import { DeltaOp, isDeltaBlank, quillDeltaToMarkdown } from '../../functions/quill-delta-to-markdown.function';
import { FeedbackService, ProblemReportReceipt } from '../../services/feedback-service/feedback.service';

/** Quill's uploader module: the clipboard and drop handlers hand it every image file. */
interface QuillUploader {
  options: { mimetypes?: string[]; handler?: (range: unknown, files: File[]) => void };
}

/** The slice of Quill this dialog touches; the real instance arrives from the editor's `onInit`. */
interface QuillLike {
  root: HTMLElement;
  getModule(name: 'toolbar'): { addHandler(name: string, fn: () => void): void } | undefined;
  getModule(name: 'uploader'): QuillUploader | undefined;
  getContents(): { ops: DeltaOp[] };
  getSelection(focus?: boolean): { index: number; length: number } | null;
  getLength(): number;
  insertEmbed(index: number, type: string, value: string, source?: string): unknown;
  insertText(index: number, text: string, source?: string): unknown;
  setSelection(index: number, length: number, source?: string): unknown;
  setContents(delta: unknown, source?: string): unknown;
}

export const MAX_SCREENSHOTS = 5;

/** What the upload endpoint accepts; Quill's own default stops at PNG and JPEG. */
export const SCREENSHOT_MIME_TYPES = ['image/png', 'image/jpeg', 'image/webp'];

/**
 * «Повідомити про проблему»: what happened, in the person's words with formatting and pictures,
 * then the steps and the expectation as a bug report wants them. Everything leaves as Markdown;
 * pictures go up first and come back as addresses, pasted straight into the text.
 *
 * The report lands in a public tracker, and the dialog says so before the person types.
 */
@Component({
  selector: 'app-report-problem-dialog',
  imports: [FormsModule, DialogModule, EditorModule, InputTextModule, TextareaModule, ButtonModule, MessageModule],
  templateUrl: './report-problem-dialog.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './report-problem-dialog.css'
})
export class ReportProblemDialogComponent {
  private readonly feedback = inject(FeedbackService);
  private readonly router = inject(Router);
  private quill: QuillLike | null = null;

  readonly visible = model(false);

  readonly title = signal('');
  readonly steps = signal('');
  readonly expected = signal('');
  readonly screenshots = signal<string[]>([]);
  readonly isUploading = signal(false);
  readonly isSending = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly receipt = signal<ProblemReportReceipt | null>(null);
  /** Flipped on every text change so the disabled state of «Надіслати» follows the editor. */
  readonly hasDescription = signal(false);

  /** Bold, lists and code: what a bug report needs and nothing that turns into noise on GitHub. */
  readonly editorFormats = ['bold', 'italic', 'strike', 'code', 'code-block', 'list', 'link', 'image', 'header', 'blockquote', 'indent'];

  readonly maxScreenshots = MAX_SCREENSHOTS;

  onEditorInit(event: { editor: QuillLike }): void {
    this.quill = event.editor;

    // The toolbar's picture button, Ctrl+V and drag-and-drop all end in the same upload: no data:
    // URLs inside the text, only what the server stored. Quill's clipboard listener is registered
    // before any of ours and would otherwise inline the pasted file as base64 through its uploader,
    // so the uploader itself is pointed at the server.
    this.quill.getModule('toolbar')?.addHandler('image', () => this.pickScreenshot());
    const uploader = this.quill.getModule('uploader');
    if (uploader) {
      uploader.options.mimetypes = SCREENSHOT_MIME_TYPES;
      uploader.options.handler = (_range, files) => files.forEach(file => this.attach(file));
    }
  }

  onTextChange(): void {
    this.hasDescription.set(!!this.quill && !isDeltaBlank(this.quill.getContents().ops));
  }

  pickScreenshot(): void {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/png,image/jpeg,image/webp';
    input.addEventListener('change', () => {
      const file = input.files?.[0];
      if (file) {
        this.attach(file);
      }
    });
    input.click();
  }

  onFilePicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.attach(file);
    }
    input.value = '';
  }

  attach(file: File): void {
    if (this.screenshots().length >= MAX_SCREENSHOTS) {
      this.errorMessage.set(`Не більше ${MAX_SCREENSHOTS} скриншотів на одне повідомлення.`);
      return;
    }

    this.isUploading.set(true);
    this.errorMessage.set(null);
    this.feedback.uploadScreenshot(file).subscribe({
      next: ({ url }) => {
        this.isUploading.set(false);
        this.screenshots.update(list => [...list, url]);
        this.insertImage(url);
      },
      error: (error: unknown) => {
        this.isUploading.set(false);
        this.errorMessage.set(failureDetail(error, 'Не вдалося завантажити скриншот. PNG, JPEG або WebP до 5 МБ.'));
      }
    });
  }

  removeScreenshot(url: string): void {
    this.screenshots.update(list => list.filter(item => item !== url));
  }

  send(): void {
    if (!this.quill || !this.hasDescription()) {
      return;
    }

    this.isSending.set(true);
    this.errorMessage.set(null);

    this.feedback.reportProblem({
      title: this.title().trim() || null,
      description: quillDeltaToMarkdown(this.quill.getContents().ops),
      steps: this.steps().trim() || null,
      expected: this.expected().trim() || null,
      route: this.router.url,
      appVersion: environment.version,
      screenshotUrls: this.screenshots()
    }).subscribe({
      next: receipt => {
        this.isSending.set(false);
        this.receipt.set(receipt);
      },
      error: (error: unknown) => {
        this.isSending.set(false);
        this.errorMessage.set(failureDetail(error, 'Не вдалося надіслати. Спробуй ще раз за хвилину.'));
      }
    });
  }

  close(): void {
    this.visible.set(false);
    if (this.receipt()) {
      this.reset();
    }
  }

  private reset(): void {
    this.title.set('');
    this.steps.set('');
    this.expected.set('');
    this.screenshots.set([]);
    this.receipt.set(null);
    this.errorMessage.set(null);
    this.hasDescription.set(false);
    this.quill?.setContents([], 'api');
  }

  private insertImage(url: string): void {
    if (!this.quill) {
      return;
    }

    // On its own line, so the Markdown reads «text, then picture» rather than a picture glued to
    // the end of a sentence.
    let at = this.quill.getSelection(true)?.index ?? this.quill.getLength() - 1;
    const beforeCursor = quillDeltaToMarkdown(this.quill.getContents().ops).length > 0 && at > 0;
    if (beforeCursor) {
      this.quill.insertText(at, '\n', 'user');
      at += 1;
    }
    this.quill.insertEmbed(at, 'image', url, 'user');
    this.quill.insertText(at + 1, '\n', 'user');
    this.quill.setSelection(at + 2, 0, 'api');
    this.onTextChange();
  }
}
