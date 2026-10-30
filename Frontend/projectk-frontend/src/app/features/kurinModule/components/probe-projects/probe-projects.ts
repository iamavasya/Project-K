import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from '@openng/optimus-ui/button';
import { CheckboxModule } from '@openng/optimus-ui/checkbox';
import { ConfirmationService } from '@openng/optimus-ui/api';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { EditorModule } from '@openng/optimus-ui/editor';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { SkeletonModule } from '@openng/optimus-ui/skeleton';
import { TagModule } from '@openng/optimus-ui/tag';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state';
import { failureDetail } from '../../../../shared/functions/failure-detail.function';
import { formatUtcDateTime } from '../../../../shared/functions/utc-date-time.function';
import { DeltaOp, quillDeltaToMarkdown } from '../../../systemModule/functions/quill-delta-to-markdown.function';
import { hasContent, markdownBlocksToDelta, MarkdownBlock, parseProjectMarkdown } from '../../functions/project-markdown.function';
import {
  PROJECT_IMAGE_SCHEME,
  ProbeProjectDto,
  ProbeProjectsDto,
  SaveProbeProjectRequestDto
} from '../../models/probes-and-badges/probe-project.dto';
import { MemberProgressService } from '../../services/probes-and-badges/member-progress.service';

/** The slice of Quill the editor touches; the instance arrives from `onInit`. */
interface QuillLike {
  getModule(name: 'toolbar'): { addHandler(name: string, fn: () => void): void } | undefined;
  getModule(name: 'uploader'): { options: { mimetypes?: string[]; handler?: (range: unknown, files: File[]) => void } } | undefined;
  getContents(): { ops: DeltaOp[] };
  getSelection(focus?: boolean): { index: number; length: number } | null;
  getLength(): number;
  insertEmbed(index: number, type: string, value: string, source?: string): unknown;
  insertText(index: number, text: string, source?: string): unknown;
  setSelection(index: number, length: number, source?: string): unknown;
  setContents(delta: unknown, source?: string): unknown;
}

/** One project as the page shows it. */
interface ProjectView {
  project: ProbeProjectDto;
  blocks: MarkdownBlock[];
  hasDescription: boolean;
  updatedLabel: string;
}

export const PROJECT_IMAGE_MIME_TYPES = ['image/png', 'image/jpeg', 'image/webp'];

const REFUSALS: Record<string, string> = {
  ImageTooLarge: 'Зображення завелике.',
  UnsupportedImageType: 'Підходять лише PNG, JPEG або WebP.',
  InvalidImageContent: 'Це не зображення, або файл пошкоджений.',
  TooManyImages: 'У проєкті вже повний набір зображень. Прибери зайве й збережи, тоді можна додати нове.',
  TooManyProjects: 'Проєктів уже стільки, скільки передбачає проба.',
  TitleRequired: 'Дай проєкту назву.',
  TitleTooLong: 'Назва задовга.',
  DescriptionTooLong: 'Опис задовгий. Скороти його.',
  MentorTooLong: 'Імʼя чи контакт ментора задовгі.',
  ForeignImage: 'Зображення можна лише завантажити в проєкт, а не вставити посиланням.',
  NotProjectProbe: 'Проєкти є лише в третій пробі.'
};

/**
 * «Проєкти» of the third probe: up to five, one of them individual, each described with
 * formatting and a few small pictures. The description leaves the editor as Markdown — the same
 * path as «Повідомити про проблему» — with pictures written as keys, since their links expire.
 * Who may write comes from the server: the person themself, and whoever may sign their probe.
 */
@Component({
  selector: 'app-probe-projects',
  imports: [NgTemplateOutlet, FormsModule, ButtonModule, CheckboxModule, DialogModule, EditorModule, InputTextModule, SkeletonModule, TagModule, EmptyStateComponent],
  templateUrl: './probe-projects.html',
  styleUrl: './probe-projects.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProbeProjectsComponent {
  private readonly progress = inject(MemberProgressService);
  private readonly confirmation = inject(ConfirmationService);
  private quill: QuillLike | null = null;
  /** Picture links in the editor and the keys they stand for. */
  private readonly imageKeys = new Map<string, string>();

  readonly memberKey = input.required<string>();
  readonly probeId = input.required<string>();
  /** A senior's probe is their archive: shown, never written. */
  readonly readOnly = input(false);

  readonly data = signal<ProbeProjectsDto | null>(null);
  readonly isLoading = signal(false);
  readonly loadFailed = signal(false);

  readonly dialogVisible = signal(false);
  readonly editingKey = signal<string | null>(null);
  readonly title = signal('');
  readonly isIndividual = signal(false);
  readonly mentorName = signal('');
  readonly mentorContact = signal('');
  readonly imageCount = signal(0);
  readonly descriptionLength = signal(0);
  readonly isSaving = signal(false);
  readonly isUploading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly editorFormats = ['bold', 'italic', 'strike', 'code', 'code-block', 'list', 'link', 'image', 'header', 'blockquote', 'indent'];
  readonly imageAccept = PROJECT_IMAGE_MIME_TYPES.join(',');

  readonly canEdit = computed(() => !this.readOnly() && !!this.data()?.canEdit);
  readonly limits = computed(() => this.data()?.limits ?? null);
  readonly maxImageLabel = computed(() => megabytes(this.limits()?.maxImageBytes ?? 0));
  readonly projects = computed<ProjectView[]>(() => (this.data()?.projects ?? []).map(project => this.view(project)));
  readonly canAdd = computed(() => this.canEdit() && this.projects().length < (this.limits()?.maxProjects ?? 0));
  readonly hasIndividual = computed(() => this.projects().some(view => view.project.isIndividual));

  constructor() {
    effect(() => this.load(this.memberKey(), this.probeId()));
  }

  openNew(): void {
    this.openEditor(null);
  }

  edit(project: ProbeProjectDto): void {
    this.openEditor(project);
  }

  remove(project: ProbeProjectDto): void {
    this.confirmation.confirm({
      header: 'Видалити проєкт',
      message: `Проєкт «${project.title}» зникне разом з описом і зображеннями.`,
      icon: 'pi pi-exclamation-triangle',
      rejectButtonProps: { label: 'Скасувати', severity: 'secondary', outlined: true },
      acceptButtonProps: { label: 'Видалити', severity: 'danger' },
      accept: () => this.progress.deleteProbeProject(this.memberKey(), this.probeId(), project.projectKey).subscribe({
        next: () => this.data.update(data => data && { ...data, projects: data.projects.filter(p => p.projectKey !== project.projectKey) })
      })
    });
  }

  onEditorInit(event: { editor: QuillLike }): void {
    this.quill = event.editor;
    this.quill.getModule('toolbar')?.addHandler('image', () => this.pickImage());
    const uploader = this.quill.getModule('uploader');
    if (uploader) {
      uploader.options.mimetypes = PROJECT_IMAGE_MIME_TYPES;
      uploader.options.handler = (_range, files) => files.forEach(file => this.attach(file));
    }
    this.fillEditor();
  }

  onTextChange(): void {
    const markdown = this.currentMarkdown();
    this.descriptionLength.set(markdown.length);
    this.imageCount.set(this.quill?.getContents().ops.filter(op => typeof op.insert !== 'string' && op.insert.image).length ?? 0);
  }

  pickImage(): void {
    const picker = document.createElement('input');
    picker.type = 'file';
    picker.accept = this.imageAccept;
    picker.addEventListener('change', () => {
      const file = picker.files?.[0];
      if (file) {
        this.attach(file);
      }
    });
    picker.click();
  }

  save(): void {
    const request = this.request();
    if (!request) {
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    const key = this.editingKey();
    const call = key
      ? this.progress.updateProbeProject(this.memberKey(), this.probeId(), key, request)
      : this.progress.addProbeProject(this.memberKey(), this.probeId(), request);

    call.subscribe({
      next: saved => {
        this.isSaving.set(false);
        this.store(saved);
        this.dialogVisible.set(false);
      },
      error: (error: unknown) => {
        this.isSaving.set(false);
        this.errorMessage.set(refusal(error, 'Не вдалося зберегти. Спробуй ще раз.'));
      }
    });
  }

  close(): void {
    this.dialogVisible.set(false);
  }

  private load(memberKey: string, probeId: string): void {
    this.isLoading.set(true);
    this.loadFailed.set(false);
    this.progress.getProbeProjects(memberKey, probeId).subscribe({
      next: data => {
        this.data.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.data.set(null);
        this.loadFailed.set(true);
        this.isLoading.set(false);
      }
    });
  }

  private view(project: ProbeProjectDto): ProjectView {
    const blocks = parseProjectMarkdown(project.description, target => imageUrl(project, target));
    const when = formatUtcDateTime(project.updatedAtUtc);
    return {
      project,
      blocks,
      hasDescription: hasContent(blocks),
      updatedLabel: [when ? `Оновлено ${when}` : null, project.updatedByName].filter(Boolean).join(' · ')
    };
  }

  private openEditor(project: ProbeProjectDto | null): void {
    this.editingKey.set(project?.projectKey ?? null);
    this.title.set(project?.title ?? '');
    this.isIndividual.set(project?.isIndividual ?? !this.hasIndividual());
    this.mentorName.set(project?.directionMentorName ?? '');
    this.mentorContact.set(project?.directionMentorContact ?? '');
    this.errorMessage.set(null);
    this.imageKeys.clear();
    project?.images.forEach(image => this.imageKeys.set(image.url, image.imageKey));
    this.dialogVisible.set(true);
    this.fillEditor();
  }

  /** The editor exists only while the dialog is open, so it is filled from whichever comes last. */
  private fillEditor(): void {
    if (!this.quill || !this.dialogVisible()) {
      return;
    }

    const project = this.data()?.projects.find(p => p.projectKey === this.editingKey());
    const blocks = project ? parseProjectMarkdown(project.description, target => imageUrl(project, target)) : [];
    this.quill.setContents(markdownBlocksToDelta(blocks), 'api');
    this.onTextChange();
  }

  private currentMarkdown(): string {
    if (!this.quill) {
      return '';
    }

    return quillDeltaToMarkdown(this.quill.getContents().ops, {
      imageAlt: 'зображення',
      imageTarget: src => {
        const key = this.imageKeys.get(src);
        return key ? `${PROJECT_IMAGE_SCHEME}${key}` : null;
      }
    });
  }

  private request(): SaveProbeProjectRequestDto | null {
    const title = this.title().trim();
    if (!title) {
      this.errorMessage.set(REFUSALS['TitleRequired']);
      return null;
    }

    return {
      title,
      isIndividual: this.isIndividual(),
      description: this.currentMarkdown(),
      directionMentorName: this.mentorName().trim() || null,
      directionMentorContact: this.mentorContact().trim() || null
    };
  }

  /**
   * A picture goes into a project that exists. A new one is saved first, quietly, so the person
   * does not have to know about it; that needs a title.
   */
  private attach(file: File): void {
    const limits = this.limits();
    if (limits && file.size > limits.maxImageBytes) {
      this.errorMessage.set(`Зображення завелике: до ${this.maxImageLabel()} МБ.`);
      return;
    }
    if (!PROJECT_IMAGE_MIME_TYPES.includes(file.type)) {
      this.errorMessage.set(REFUSALS['UnsupportedImageType']);
      return;
    }

    const key = this.editingKey();
    if (key) {
      this.upload(key, file);
      return;
    }

    const request = this.request();
    if (!request) {
      this.errorMessage.set('Спершу дай проєкту назву — тоді можна додати зображення.');
      return;
    }

    this.isUploading.set(true);
    this.progress.addProbeProject(this.memberKey(), this.probeId(), { ...request, description: '' }).subscribe({
      next: saved => {
        this.editingKey.set(saved.projectKey);
        this.store(saved);
        this.upload(saved.projectKey, file);
      },
      error: (error: unknown) => {
        this.isUploading.set(false);
        this.errorMessage.set(refusal(error, 'Не вдалося зберегти проєкт. Спробуй ще раз.'));
      }
    });
  }

  private upload(projectKey: string, file: File): void {
    this.isUploading.set(true);
    this.errorMessage.set(null);
    this.progress.uploadProbeProjectImage(this.memberKey(), this.probeId(), projectKey, file).subscribe({
      next: image => {
        this.isUploading.set(false);
        this.imageKeys.set(image.url, image.imageKey);
        this.data.update(data => data && {
          ...data,
          projects: data.projects.map(p => p.projectKey === projectKey ? { ...p, images: [...p.images, image] } : p)
        });
        this.insertImage(image.url);
      },
      error: (error: unknown) => {
        this.isUploading.set(false);
        const limit = `PNG, JPEG або WebP до ${this.maxImageLabel()} МБ.`;
        this.errorMessage.set(error instanceof HttpErrorResponse && error.status === 413
          ? `Зображення завелике: ${limit}`
          : refusal(error, `Не вдалося завантажити зображення. ${limit}`));
      }
    });
  }

  private insertImage(url: string): void {
    if (!this.quill) {
      return;
    }

    let at = this.quill.getSelection(true)?.index ?? this.quill.getLength() - 1;
    if (at > 0) {
      this.quill.insertText(at, '\n', 'user');
      at += 1;
    }
    this.quill.insertEmbed(at, 'image', url, 'user');
    this.quill.insertText(at + 1, '\n', 'user');
    this.quill.setSelection(at + 2, 0, 'api');
    this.onTextChange();
  }

  private store(saved: ProbeProjectDto): void {
    this.data.update(data => {
      if (!data) {
        return data;
      }
      const exists = data.projects.some(p => p.projectKey === saved.projectKey);
      return {
        ...data,
        projects: exists ? data.projects.map(p => p.projectKey === saved.projectKey ? saved : p) : [...data.projects, saved]
      };
    });
  }
}

function imageUrl(project: ProbeProjectDto, target: string): string | null {
  if (!target.startsWith(PROJECT_IMAGE_SCHEME)) {
    return null;
  }
  const key = target.slice(PROJECT_IMAGE_SCHEME.length);
  return project.images.find(image => image.imageKey === key)?.url ?? null;
}

function megabytes(bytes: number): string {
  return (bytes / (1024 * 1024)).toLocaleString('uk-UA', { maximumFractionDigits: 1 });
}

function refusal(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse && error.status === 400) {
    const code = (error.error as { error?: string } | null)?.error;
    if (code && REFUSALS[code]) {
      return REFUSALS[code];
    }
  }
  return failureDetail(error, fallback);
}
