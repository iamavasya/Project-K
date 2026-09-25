import { MAX_IMAGE_BYTES, isImageTooLarge } from './image-upload.function';

describe('image upload size', () => {
  it('lets through what the API accepts and stops what it would drop', () => {
    expect(isImageTooLarge(new Blob([new Uint8Array(MAX_IMAGE_BYTES)]))).toBeFalse();
    expect(isImageTooLarge(new Blob([new Uint8Array(MAX_IMAGE_BYTES + 1)]))).toBeTrue();
    expect(isImageTooLarge(null)).toBeFalse();
  });
});
