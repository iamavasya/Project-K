/**
 * iOS Safari ignores user-scalable=no, so pinch zoom is blocked through WebKit's gesture events
 * and two-finger touchmove. Double-tap zoom is handled by touch-action in styles.scss, which keeps
 * fast repeated taps working.
 */
export function blockPinchZoom(doc: Document = document): void {
  const stop = (e: Event) => e.preventDefault();
  for (const type of ['gesturestart', 'gesturechange', 'gestureend']) {
    doc.addEventListener(type, stop, { passive: false });
  }
  doc.addEventListener(
    'touchmove',
    (e: TouchEvent) => {
      if (e.touches.length > 1) e.preventDefault();
    },
    { passive: false },
  );
}
