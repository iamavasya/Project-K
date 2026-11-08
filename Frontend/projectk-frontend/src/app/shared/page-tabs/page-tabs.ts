import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

/** One tab of a page made of several: where it leads and what it says. */
export interface PageTab {
  readonly label: string;
  /** The tab's own address, so it can be linked to and survives a reload. */
  readonly link: string;
  /** Active only on exactly this address, not on the addresses under it. */
  readonly exact?: boolean;
  /** A number beside the label, e.g. requests waiting; nothing is shown for zero. */
  readonly count?: number;
  readonly countLabel?: string;
  readonly testId?: string;
}

/**
 * The tabs of a page that gathers several (Реєстр, Календар, Вкладка, Налаштування): links in one
 * row under the title, the current one underlined. One tab is no choice, so nothing is drawn.
 */
@Component({
  selector: 'app-page-tabs',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (tabs().length > 1) {
      <nav class="page-tabs" [attr.aria-label]="label()">
        @for (tab of tabs(); track tab.link) {
          <a class="page-tabs__tab" [routerLink]="tab.link" routerLinkActive="page-tabs__tab--on"
            [routerLinkActiveOptions]="{ exact: !!tab.exact }" ariaCurrentWhenActive="page"
            [attr.data-testid]="tab.testId ?? null">
            {{ tab.label }}
            @if (tab.count) {
              <span class="page-tabs__count" [attr.aria-label]="tab.countLabel ?? null">{{ tab.count }}</span>
            }
          </a>
        }
      </nav>
    }
  `,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }

    /* On a phone the row scrolls sideways instead of wrapping. */
    .page-tabs {
      display: flex;
      gap: 0.25rem;
      border-bottom: 1px solid var(--lil-line);
      overflow-x: auto;
      scrollbar-width: none;
    }

    .page-tabs__tab {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.625rem 0.875rem;
      border-bottom: 2px solid transparent;
      margin-bottom: -1px;
      color: var(--lil-muted);
      font-weight: var(--lil-weight-ui, 600);
      text-decoration: none;
      white-space: nowrap;
    }

    .page-tabs__tab:hover {
      color: var(--lil-ink);
    }

    .page-tabs__tab:focus-visible {
      outline: 2px solid var(--p-primary-color);
      outline-offset: 2px;
      border-radius: var(--lil-radius);
    }

    .page-tabs__tab--on {
      color: var(--p-primary-color);
      border-bottom-color: var(--p-primary-color);
    }

    /* Something waiting: the same clay as the menu's attention dot. */
    .page-tabs__count {
      min-width: 1.25rem;
      padding: 0 0.375rem;
      border-radius: 999px;
      background: var(--lil-clay-50);
      color: var(--lil-clay-700);
      font-size: var(--lil-text-caption, 0.75rem);
      font-weight: 700;
      text-align: center;
    }
  `
})
export class PageTabsComponent {
  readonly tabs = input.required<readonly PageTab[]>();
  /** What the row is, for a screen reader: «Режими реєстру», «Розділи налаштувань». */
  readonly label = input.required<string>();
}
