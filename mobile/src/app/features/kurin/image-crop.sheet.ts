import { Component, ElementRef, OnDestroy, OnInit, afterNextRender, computed, inject, signal, viewChild } from '@angular/core';
import {
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonItem,
  IonItemGroup,
  IonList,
  IonNote,
  IonSpinner,
  IonTitle,
  IonToggle,
  IonToolbar,
  ModalController,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { add, remove } from 'ionicons/icons';
import { Toasts } from '../../core/toast';
import {
  CropView,
  DEFAULT_SILHOUETTE,
  IMAGE_TOO_LARGE,
  MAX_IMAGE_BYTES,
  MAX_ZOOM,
  SILHOUETTE_SIZE,
  clampView,
  cropPhoto,
  cropSilhouette,
  initialView,
  loadImage,
  renamed,
  zoomAround,
  zoomOf,
} from './image-crop';

/** What the sheet hands back: the picture to upload and the name to send it under. */
export interface CroppedImage {
  blob: Blob;
  name: string;
}

/**
 * Cropping a picture to a square before it goes up (web: the image-cropper dialogs of upsert-member
 * and group-panel). Drag to move, pinch or the slider to zoom; a profile photo comes out as a JPEG of
 * at most 1024px, a гурток's silhouette as the web's processed 512px circle, with its «Чорно-біле»,
 * «Обводка» and «Без обробки». Dismisses with a CroppedImage, or null.
 */
@Component({
  selector: 'app-image-crop-sheet',
  imports: [
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonButton,
    IonContent,
    IonIcon,
      IonList,
    IonItemGroup,
    IonItem,
    IonToggle,
    IonNote,
    IonSpinner,
  ],
  styles: `
    .stage {
      position: relative;
      width: min(100% - 32px, 360px);
      aspect-ratio: 1;
      margin: 16px auto 0;
      overflow: hidden;
      border-radius: 12px;
      background: #000;
      touch-action: none;
      cursor: grab;
      user-select: none;
      -webkit-user-select: none;
    }
    .stage img {
      position: absolute;
      top: 0;
      left: 0;
      max-width: none;
      transform-origin: 0 0;
      pointer-events: none;
    }
    .stage img.gray {
      filter: grayscale(1);
    }
    /* The round avatar or silhouette the square will become, with the web's ring when it has one. */
    .mask {
      position: absolute;
      inset: 0;
      border-radius: 50%;
      pointer-events: none;
      box-shadow:
        inset 0 0 0 var(--ring, 0px) #101413,
        0 0 0 999px rgba(0, 0, 0, 0.5);
    }
    .stage ion-spinner {
      position: absolute;
      inset: 0;
      margin: auto;
      color: #fff;
    }
    .zoom {
      display: flex;
      align-items: center;
      gap: 8px;
      width: min(100% - 32px, 360px);
      margin: 8px auto 0;
      color: var(--lk-muted);
    }
    /* Native sliders, as the web's silhouette dialog has: a system control on both platforms. */
    input[type='range'] {
      flex: 1;
      min-width: 0;
      margin: 0;
      accent-color: var(--lk-primary);
    }
    .hint {
      margin: 0 20px;
      font-size: 13px;
      text-align: center;
      color: var(--lk-muted);
    }
    .thickness + ion-note {
      min-width: 2ch;
      margin-inline-start: 12px;
      text-align: end;
    }
    .thickness {
      display: flex;
      flex: 1;
      align-items: center;
      gap: 12px;
      min-height: 44px;
    }
    .actions {
      padding: 0 16px 24px;
    }
  `,
  template: `
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-button [disabled]="busy()" (click)="close(null)">Скасувати</ion-button>
        </ion-buttons>
        <ion-title>{{ silhouette ? 'Сильветка' : 'Обріж фото' }}</ion-title>
        <ion-buttons slot="end">
          <ion-button [strong]="true" [disabled]="!ready() || busy()" (click)="done()" data-testid="crop-done">Готово</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <!-- The picture is larger than its window on purpose; the window clips it (layout audit: free). -->
      <div
        #stage
        class="stage"
        data-testid="crop-stage"
        data-layout-free
        (pointerdown)="down($event)"
        (pointermove)="move($event)"
        (pointerup)="up($event)"
        (pointercancel)="up($event)"
        (wheel)="wheel($event)"
      >
        @if (src(); as source) {
          <img
            [src]="source"
            alt=""
            draggable="false"
            [class.gray]="silhouette && grayscale()"
            [style.width.px]="size().width"
            [style.height.px]="size().height"
            [style.transform]="transform()"
          />
        } @else {
          <ion-spinner name="crescent" />
        }
        <div class="mask" [style.--ring]="ring()"></div>
      </div>
      <div class="zoom">
        <ion-icon name="remove" aria-hidden="true" />
        <input
          type="range"
          aria-label="Масштаб"
          min="1"
          [max]="maxZoom"
          step="0.01"
          [value]="zoom()"
          [disabled]="!ready()"
          (input)="setZoom(+$any($event.target).value)"
          data-testid="crop-zoom"
        />
        <ion-icon name="add" aria-hidden="true" />
      </div>
      <p class="hint">Посунь і наблизь, щоб у колі було те, що треба.</p>

      @if (silhouette) {
        <ion-list [inset]="true" data-testid="silhouette-options">
          <ion-item-group>
            <ion-item>
              <ion-toggle [checked]="grayscale()" justify="space-between" (ionChange)="grayscale.set($any($event).detail.checked)">Чорно-біле</ion-toggle>
            </ion-item>
            <ion-item>
              <ion-toggle [checked]="outlineOn()" justify="space-between" (ionChange)="outlineOn.set($any($event).detail.checked)">Обводка</ion-toggle>
            </ion-item>
            <ion-item>
              <label class="thickness">
                <span>Товщина</span>
                <input
                  type="range"
                  min="0"
                  max="32"
                  step="1"
                  [value]="outline()"
                  [disabled]="!outlineOn()"
                  (input)="outline.set(+$any($event.target).value)"
                />
              </label>
              <ion-note slot="end">{{ outline() }}</ion-note>
            </ion-item>
          </ion-item-group>
        </ion-list>
        <div class="actions">
          <ion-button expand="block" fill="clear" [disabled]="busy()" (click)="original()">Без обробки</ion-button>
        </div>
      }
    </ion-content>
  `,
})
export class ImageCropSheet implements OnInit, OnDestroy {
  private readonly modals = inject(ModalController);
  private readonly toasts = inject(Toasts);
  private readonly stage = viewChild<ElementRef<HTMLElement>>('stage');

  // Set by ModalController's componentProps.
  file: File | null = null;
  /** A гурток's silhouette rather than a profile photo. */
  silhouette = false;

  protected readonly maxZoom = MAX_ZOOM;
  protected readonly src = signal<string | null>(null);
  protected readonly size = signal({ width: 0, height: 0 });
  private readonly side = signal(0);
  private readonly view = signal<CropView>({ x: 0, y: 0, scale: 1 });
  protected readonly busy = signal(false);
  protected readonly grayscale = signal(DEFAULT_SILHOUETTE.grayscale);
  protected readonly outlineOn = signal(true);
  protected readonly outline = signal(DEFAULT_SILHOUETTE.outline);

  protected readonly ready = computed(() => !!this.src() && this.side() > 0);
  protected readonly transform = computed(() => {
    const v = this.view();
    return `translate(${v.x}px, ${v.y}px) scale(${v.scale})`;
  });
  protected readonly zoom = computed(() => (this.ready() ? zoomOf(this.view(), this.size().width, this.size().height, this.side()) : 1));
  /** The ring as the 512px picture will have it, scaled to the window. */
  protected readonly ring = computed(() =>
    this.silhouette && this.outlineOn() ? `${(this.outline() * this.side()) / SILHOUETTE_SIZE}px` : '0px',
  );

  private image: HTMLImageElement | null = null;
  private readonly pointers = new Map<number, { x: number; y: number }>();
  private observer: ResizeObserver | null = null;

  constructor() {
    addIcons({ add, remove });
    afterNextRender(() => {
      const stage = this.stage()?.nativeElement;
      if (!stage) return;
      this.measure(stage);
      this.observer = new ResizeObserver(() => this.measure(stage));
      this.observer.observe(stage);
    });
  }

  ngOnInit(): void {
    void this.load();
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    if (this.image) URL.revokeObjectURL(this.image.src);
  }

  protected close(result: CroppedImage | null): void {
    void this.modals.dismiss(result);
  }

  protected down(event: PointerEvent): void {
    if (!this.ready()) return;
    (event.currentTarget as HTMLElement).setPointerCapture?.(event.pointerId);
    this.pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
  }

  protected move(event: PointerEvent): void {
    const before = this.pointers.get(event.pointerId);
    if (!before) return;
    const others = [...this.pointers.entries()].filter(([id]) => id !== event.pointerId).map(([, p]) => p);
    const now = { x: event.clientX, y: event.clientY };
    this.pointers.set(event.pointerId, now);
    const { width, height } = this.size();
    const side = this.side();
    if (!others.length) {
      const v = this.view();
      this.view.set(clampView({ ...v, x: v.x + now.x - before.x, y: v.y + now.y - before.y }, width, height, side));
      return;
    }
    // Two fingers: zoom by how far they moved apart, about the point between them, and follow it.
    const other = others[0];
    const apart = (a: { x: number; y: number }) => Math.hypot(a.x - other.x, a.y - other.y) || 1;
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const mid = (a: { x: number; y: number }) => ({ x: (a.x + other.x) / 2 - rect.left, y: (a.y + other.y) / 2 - rect.top });
    const from = mid(before);
    const to = mid(now);
    const zoomed = zoomAround(this.view(), apart(now) / apart(before), from.x, from.y, width, height, side);
    this.view.set(clampView({ ...zoomed, x: zoomed.x + to.x - from.x, y: zoomed.y + to.y - from.y }, width, height, side));
  }

  protected up(event: PointerEvent): void {
    this.pointers.delete(event.pointerId);
  }

  protected wheel(event: WheelEvent): void {
    if (!this.ready()) return;
    event.preventDefault();
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const { width, height } = this.size();
    this.view.set(zoomAround(this.view(), Math.exp(-event.deltaY / 300), event.clientX - rect.left, event.clientY - rect.top, width, height, this.side()));
  }

  protected setZoom(value: number): void {
    if (!this.ready() || typeof value !== 'number') return;
    const { width, height } = this.size();
    const side = this.side();
    this.view.set(zoomAround(this.view(), value / this.zoom(), side / 2, side / 2, width, height, side));
  }

  protected async done(): Promise<void> {
    const image = this.image;
    if (!image || !this.ready() || this.busy()) return;
    this.busy.set(true);
    try {
      const blob = this.silhouette
        ? await cropSilhouette(image, this.view(), this.side(), { grayscale: this.grayscale(), outline: this.outlineOn() ? this.outline() : 0 })
        : await cropPhoto(image, this.view(), this.side());
      if (blob.size > MAX_IMAGE_BYTES) {
        await this.toasts.show(IMAGE_TOO_LARGE, 'danger');
        return;
      }
      const name = this.silhouette ? renamed(this.file?.name, 'png', 'silhouette') : renamed(this.file?.name, 'jpg', 'profile');
      this.close({ blob, name });
    } catch {
      await this.toasts.show('Не вдалося обробити зображення.', 'danger');
    } finally {
      this.busy.set(false);
    }
  }

  /** «Без обробки»: the picture as it was picked (the web checked its type and size on picking). */
  protected original(): void {
    const file = this.file;
    if (file) this.close({ blob: file, name: file.name || 'silhouette.png' });
  }

  private async load(): Promise<void> {
    if (!this.file) return this.close(null);
    try {
      this.image = await loadImage(this.file);
    } catch {
      await this.toasts.show('Не вдалося відкрити це зображення. Обери інше.', 'danger');
      this.close(null);
      return;
    }
    this.size.set({ width: this.image.naturalWidth, height: this.image.naturalHeight });
    this.src.set(this.image.src);
    this.reset();
  }

  private measure(stage: HTMLElement): void {
    const side = stage.clientWidth;
    const before = this.side();
    if (!side || side === before) return;
    this.side.set(side);
    if (!before) return this.reset();
    // A new window size keeps the same part of the picture in view.
    const k = side / before;
    const v = this.view();
    this.view.set(clampView({ x: v.x * k, y: v.y * k, scale: v.scale * k }, this.size().width, this.size().height, side));
  }

  private reset(): void {
    const { width, height } = this.size();
    if (width && this.side()) this.view.set(initialView(width, height, this.side()));
  }
}
