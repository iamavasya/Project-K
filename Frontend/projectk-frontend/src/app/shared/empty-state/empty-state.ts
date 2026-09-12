import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type EmptyStateArt = 'trail' | 'list' | 'calendar';

/**
 * Empty state per BRANDBOOK §5–§6: handwritten title, one explaining line, one action.
 * The action is projected, so a caller without a permitted action simply passes nothing.
 */
@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="lil-empty">
      <svg class="lil-empty__art" viewBox="0 0 110 66" fill="none" stroke="currentColor"
           stroke-width="2" stroke-linecap="round" aria-hidden="true">
        @switch (art()) {
          @case ('list') {
            <path d="M18 18 H92" />
            <path d="M18 33 H92" />
            <path class="lil-empty__accent" d="M18 48 H54" />
            <circle cx="10" cy="18" r="3" />
            <circle cx="10" cy="33" r="3" />
            <circle cx="10" cy="48" r="3" />
          }
          @case ('calendar') {
            <rect x="18" y="14" width="74" height="42" rx="4" />
            <path d="M18 27 H92" />
            <path class="lil-empty__accent" d="M34 41 H54" />
            <path d="M34 8 V16" />
            <path d="M76 8 V16" />
          }
          @default {
            <path class="lil-empty__dash" d="M8 48 C28 26 48 56 68 32 C82 16 94 28 102 22" />
            <circle class="lil-empty__accent" cx="102" cy="22" r="4" />
          }
        }
      </svg>
      <div class="lil-empty__title">{{ title() }}</div>
      @if (body()) {
        <div class="lil-empty__body">{{ body() }}</div>
      }
      <ng-content />
    </div>
  `
})
export class EmptyStateComponent {
  readonly title = input.required<string>();
  readonly body = input<string>('');
  readonly art = input<EmptyStateArt>('trail');
}
