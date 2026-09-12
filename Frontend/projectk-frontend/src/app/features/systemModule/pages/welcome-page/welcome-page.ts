import { Component, ChangeDetectionStrategy, DestroyRef, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { DividerModule } from '@openng/optimus-ui/divider';
import { TagModule } from '@openng/optimus-ui/tag';
import { environment } from '../../../../../environments/environment';

/** One frame of the product preview on the welcome page. */
export interface PreviewScreen {
  src: string;
  alt: string;
  /** What the window bar says while this frame is up. */
  title: string;
}

/** How long a frame stays up before the next one; the fade itself is 200ms (BRANDBOOK §4). */
export const PREVIEW_FRAME_MS = 6000;

@Component({
  selector: 'app-welcome-page',
  imports: [RouterLink, ButtonModule, DividerModule, TagModule],
  templateUrl: './welcome-page.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './welcome-page.css'
})
export class WelcomePageComponent {
  // The one place BRANDBOOK §0 still allows the technical name next to the version.
  readonly techLine = `ProjectK · ${environment.envName} · ${environment.version}`;

  readonly screens: PreviewScreen[] = [
    { src: 'assets/images/software-screens/kurin.png', alt: 'Сторінка куреня: гуртки, КВ і провід', title: 'Курінь' },
    { src: 'assets/images/software-screens/registry.png', alt: 'Реєстр куреня: юнаки за гуртками зі ступенями', title: 'Реєстр' },
    { src: 'assets/images/software-screens/member.png', alt: 'Картка юнака: проби, вмілості, відзначення', title: 'Картка юнака' }
  ];

  readonly active = signal(0);

  private timer: ReturnType<typeof setInterval> | null = null;

  constructor() {
    // Frames advance on their own unless the person asked for less motion; picking a dot by
    // hand restarts the clock so the chosen frame is not swapped out a second later.
    if (!this.prefersReducedMotion()) {
      this.startClock();
    }
    inject(DestroyRef).onDestroy(() => this.stopClock());
  }

  show(index: number): void {
    this.active.set(index);
    if (this.timer) {
      this.startClock();
    }
  }

  private startClock(): void {
    this.stopClock();
    this.timer = setInterval(() => this.active.update(i => (i + 1) % this.screens.length), PREVIEW_FRAME_MS);
  }

  private stopClock(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  private prefersReducedMotion(): boolean {
    return typeof window !== 'undefined'
      && typeof window.matchMedia === 'function'
      && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }
}
