import { registerSegmentEffect, registerTabBarEffect, type registeredEffect } from '@rdlabo/ionic-theme-ios27';

/**
 * The Liquid Glass motion rdlabo adds by script: the lens that follows a finger across segments
 * and tab bars. Each effect checks for the ios class itself, so on Android this does nothing.
 * Controls that come and go with data are picked up on every sync.
 */
export class GlassEffects {
  private readonly effects = new Map<HTMLElement, registeredEffect>();

  constructor(private readonly host: HTMLElement) {}

  sync(): void {
    const controls = new Set(this.host.querySelectorAll<HTMLElement>('ion-segment, ion-tab-bar'));
    for (const [control, effect] of this.effects) {
      if (!controls.has(control)) {
        effect.destroy();
        this.effects.delete(control);
      }
    }
    for (const control of controls) {
      if (this.effects.has(control)) continue;
      // Ionic sets the mode class once the element is ready, and the effects check for it.
      void (control as HTMLIonSegmentElement).componentOnReady().then(() => {
        if (!control.isConnected || this.effects.has(control)) return;
        const register = control.tagName === 'ION-TAB-BAR' ? registerTabBarEffect : registerSegmentEffect;
        const effect = register(control);
        if (effect) this.effects.set(control, effect);
      });
    }
  }

  destroy(): void {
    for (const effect of this.effects.values()) effect.destroy();
    this.effects.clear();
  }
}
