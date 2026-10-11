import { MAX_ZOOM, clampView, coverScale, initialView, outputSide, renamed, sourceSquare, zoomAround, zoomOf } from './image-crop';

describe('image crop geometry', () => {
  it('starts with a landscape picture centred and just covering the window', () => {
    const view = initialView(2000, 1000, 300);
    expect(view.scale).toBeCloseTo(0.3);
    expect(view.y).toBe(0);
    expect(view.x).toBeCloseTo(-150);
    const square = sourceSquare(view, 300);
    expect(square.sx).toBeCloseTo(500);
    expect(square.sy).toBeCloseTo(0);
    expect(square.size).toBeCloseTo(1000);
  });

  it('never lets the window run off the picture or zoom out past covering it', () => {
    const side = 300;
    expect(clampView({ x: 40, y: -10, scale: 0.3 }, 2000, 1000, side)).toEqual({ x: 0, y: 0, scale: 0.3 });
    expect(clampView({ x: -9000, y: 0, scale: 0.3 }, 2000, 1000, side).x).toBeCloseTo(-300);
    expect(clampView({ x: 0, y: 0, scale: 0.01 }, 2000, 1000, side).scale).toBeCloseTo(coverScale(2000, 1000, side));
    expect(clampView({ x: 0, y: 0, scale: 99 }, 2000, 1000, side).scale).toBeCloseTo(0.3 * MAX_ZOOM);
  });

  it('zooms about a point, keeping it under the finger', () => {
    const side = 300;
    const start = initialView(1000, 1000, side);
    const zoomed = zoomAround(start, 2, 150, 150, 1000, 1000, side);
    expect(zoomOf(zoomed, 1000, 1000, side)).toBeCloseTo(2);
    // The centre of the picture stays in the centre of the window.
    const { sx, size } = sourceSquare(zoomed, side);
    expect(sx + size / 2).toBeCloseTo(500);
  });

  it('saves the crop as it is, only ever scaled down to 1024px', () => {
    expect(outputSide(3000)).toBe(1024);
    expect(outputSide(640.4)).toBe(640);
    expect(outputSide(0.2)).toBe(1);
  });

  it('names the saved picture after the picked one, with its new type', () => {
    expect(renamed('IMG_0042.HEIC', 'jpg', 'profile')).toBe('IMG_0042.jpg');
    expect(renamed('', 'png', 'silhouette')).toBe('silhouette.png');
    expect(renamed(null, 'jpg', 'profile')).toBe('profile.jpg');
  });
});
