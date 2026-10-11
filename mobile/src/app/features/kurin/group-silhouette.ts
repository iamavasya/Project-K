import { Component, effect, input, signal } from '@angular/core';
import { IonIcon } from '@ionic/angular';
import { addIcons } from 'ionicons';
import { people } from 'ionicons/icons';

/**
 * A гурток's silhouette (web group-panel), whole and uncropped on the brand tint; the people icon when
 * the гурток has none or the picture does not load.
 */
@Component({
  selector: 'app-group-silhouette',
  imports: [IonIcon],
  styles: `
    :host {
      display: inline-flex;
      flex: none;
      align-items: center;
      justify-content: center;
      width: var(--size, 40px);
      height: var(--size, 40px);
      border-radius: calc(var(--size, 40px) * 0.24);
      overflow: hidden;
      background: var(--lk-primary-50);
      color: var(--lk-primary);
      font-size: calc(var(--size, 40px) * 0.5);
    }
    img {
      width: 100%;
      height: 100%;
      object-fit: contain;
    }
  `,
  host: { 'aria-hidden': 'true', '[style.--size.px]': 'size()' },
  template: `
    @if (url() && !broken()) {
      <img [src]="url()" alt="" loading="lazy" (error)="broken.set(true)" />
    } @else {
      <ion-icon name="people" />
    }
  `,
})
export class GroupSilhouette {
  readonly url = input<string | null | undefined>(null);
  readonly size = input(40);
  protected readonly broken = signal(false);

  constructor() {
    addIcons({ people });
    // A new link (another гурток, a fresh signed URL) gets its own chance to load.
    effect(() => {
      this.url();
      this.broken.set(false);
    });
  }
}
