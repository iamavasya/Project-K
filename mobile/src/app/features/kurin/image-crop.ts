/*
 * Square cropping on the phone, in place of the web's ngx-image-cropper: the picture sits under a
 * square window, is dragged and zoomed there, and what the window shows is drawn into a canvas.
 * The geometry is pure (and unit-tested); the canvas helpers run only in the browser.
 */

/** The picture's place under the window: its top-left corner in window pixels, and window pixels per picture pixel. */
export interface CropView {
  x: number;
  y: number;
  scale: number;
}

/** How far past «just covers the window» the picture may be zoomed. */
export const MAX_ZOOM = 4;

/** The API's limit for one picture (ImageUploadRules.MaxImageBytes). */
export const MAX_IMAGE_BYTES = 5 * 1024 * 1024;

/** The types the API takes (ImageUploadRules.AllowedContentTypes). */
export const IMAGE_TYPES = ['image/png', 'image/jpeg', 'image/webp'];

/** The web's wording when a picture is over the limit (shared/functions/image-upload.function). */
export const IMAGE_TOO_LARGE = 'Фото більше за 5 МБ. Обери менше або обріж його тут.';

/** The smallest scale at which the picture still fills the whole window. */
export function coverScale(width: number, height: number, side: number): number {
  return side / Math.max(1, Math.min(width, height));
}

/** The picture centred, just covering the window. */
export function initialView(width: number, height: number, side: number): CropView {
  const scale = coverScale(width, height, side);
  return { scale, x: (side - width * scale) / 2, y: (side - height * scale) / 2 };
}

/** Keeps the zoom within its bounds and the window always over the picture. */
export function clampView(view: CropView, width: number, height: number, side: number): CropView {
  const min = coverScale(width, height, side);
  const scale = Math.min(min * MAX_ZOOM, Math.max(min, view.scale));
  const x = Math.min(0, Math.max(side - width * scale, view.x));
  const y = Math.min(0, Math.max(side - height * scale, view.y));
  return { scale, x, y };
}

/** Zooms by `factor`, keeping the point under (cx, cy) in the window where it is. */
export function zoomAround(view: CropView, factor: number, cx: number, cy: number, width: number, height: number, side: number): CropView {
  const min = coverScale(width, height, side);
  const scale = Math.min(min * MAX_ZOOM, Math.max(min, view.scale * factor));
  const ratio = scale / view.scale;
  return clampView({ scale, x: cx - (cx - view.x) * ratio, y: cy - (cy - view.y) * ratio }, width, height, side);
}

/** The zoom as the slider shows it: 1 when the picture just covers the window. */
export function zoomOf(view: CropView, width: number, height: number, side: number): number {
  return view.scale / coverScale(width, height, side);
}

/** The square of the picture, in its own pixels, that the window shows. */
export function sourceSquare(view: CropView, side: number): { sx: number; sy: number; size: number } {
  return { sx: -view.x / view.scale, sy: -view.y / view.scale, size: side / view.scale };
}

/** The side of the saved picture: the crop as it is, only ever scaled down to `max` (web resizeToWidth + onlyScaleDown). */
export function outputSide(sourceSize: number, max = 1024): number {
  return Math.max(1, Math.min(max, Math.round(sourceSize)));
}

/** «photo.heic» → «photo.jpg»: the saved picture's name with its new type (web upsert-member, group-panel). */
export function renamed(fileName: string | null | undefined, extension: 'jpg' | 'png', fallback: string): string {
  const base = (fileName ?? '').replace(/\.[^.]+$/, '');
  return `${base || fallback}.${extension}`;
}

// ── Canvas (browser only) ────────────────────────────────────────────────────────────────────────

export interface SilhouetteOptions {
  grayscale: boolean;
  /** Ring width in pixels of the 512px picture, 0 for none. */
  outline: number;
}

/** The silhouette as the web makes it: a 512px circle, by default in grey with a ring. */
export const SILHOUETTE_SIZE = 512;
export const DEFAULT_SILHOUETTE: SilhouetteOptions = { grayscale: true, outline: 8 };
/**
 * The ring's colour: the brand ink of the light theme. The picture is stored once for everyone,
 * so it must not take whatever theme the person who uploaded it happened to use.
 */
const SILHOUETTE_INK = '#101413';

/** The picture, decoded; its `src` is an object URL the caller revokes when done. */
export function loadImage(file: Blob): Promise<HTMLImageElement> {
  const url = URL.createObjectURL(file);
  const image = new Image();
  return new Promise((resolve, reject) => {
    image.onload = () => resolve(image);
    image.onerror = () => {
      URL.revokeObjectURL(url);
      reject(new Error('Image load failed.'));
    };
    image.src = url;
  });
}

/** The window's square of the picture as a JPEG, at most 1024px (the web's profile photo: quality 85). */
export function cropPhoto(image: HTMLImageElement, view: CropView, side: number): Promise<Blob> {
  const { sx, sy, size } = sourceSquare(view, side);
  const out = outputSide(size);
  const canvas = canvasOf(out);
  const ctx = canvas.getContext('2d')!;
  // A transparent PNG would turn black as a JPEG; the web's cropper lays it on white too.
  ctx.fillStyle = '#ffffff';
  ctx.fillRect(0, 0, out, out);
  ctx.drawImage(image, sx, sy, size, size, 0, 0, out, out);
  return blobOf(canvas, 'image/jpeg', 0.85);
}

/** The window's square as the web's processed silhouette: a 512px circle, greyed and ringed as asked, as PNG. */
export function cropSilhouette(image: HTMLImageElement, view: CropView, side: number, options: SilhouetteOptions): Promise<Blob> {
  const { sx, sy, size } = sourceSquare(view, side);
  const outline = Math.max(0, Math.min(32, Math.round(options.outline)));
  const full = SILHOUETTE_SIZE + outline * 2;
  const center = full / 2;
  const canvas = canvasOf(full);
  const ctx = canvas.getContext('2d')!;
  ctx.save();
  ctx.beginPath();
  ctx.arc(center, center, SILHOUETTE_SIZE / 2, 0, Math.PI * 2);
  ctx.clip();
  ctx.drawImage(image, sx, sy, size, size, outline, outline, SILHOUETTE_SIZE, SILHOUETTE_SIZE);
  ctx.restore();
  // By the pixels rather than ctx.filter, which Safari before 18 ignores.
  if (options.grayscale) grayscale(ctx, full);
  if (outline > 0) {
    ctx.beginPath();
    ctx.arc(center, center, SILHOUETTE_SIZE / 2 + outline / 2, 0, Math.PI * 2);
    ctx.lineWidth = outline;
    ctx.strokeStyle = SILHOUETTE_INK;
    ctx.stroke();
  }
  return blobOf(canvas, 'image/png');
}

function grayscale(ctx: CanvasRenderingContext2D, side: number): void {
  const data = ctx.getImageData(0, 0, side, side);
  const px = data.data;
  for (let i = 0; i < px.length; i += 4) {
    const y = Math.round(0.2126 * px[i] + 0.7152 * px[i + 1] + 0.0722 * px[i + 2]);
    px[i] = px[i + 1] = px[i + 2] = y;
  }
  ctx.putImageData(data, 0, 0);
}

function canvasOf(side: number): HTMLCanvasElement {
  const canvas = document.createElement('canvas');
  canvas.width = side;
  canvas.height = side;
  return canvas;
}

function blobOf(canvas: HTMLCanvasElement, type: string, quality?: number): Promise<Blob> {
  return new Promise((resolve, reject) =>
    canvas.toBlob((blob) => (blob ? resolve(blob) : reject(new Error('Canvas export failed.'))), type, quality),
  );
}
