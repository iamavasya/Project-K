export const MAX_IMAGE_BYTES = 5 * 1024 * 1024;

export const IMAGE_TOO_LARGE_DETAIL = 'Фото більше за 5 МБ. Обери менше або обріж його тут.';

export function isImageTooLarge(image: Blob | null | undefined): boolean {
  return !!image && image.size > MAX_IMAGE_BYTES;
}
