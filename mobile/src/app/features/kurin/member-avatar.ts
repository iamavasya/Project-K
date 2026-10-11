import { Component, computed, input, signal } from '@angular/core';
import { initials } from '../../me/labels';

/** A person's photo, or their initials on the brand green when there is none (or it fails to load). */
@Component({
  selector: 'app-member-avatar',
  styles: `
    :host {
      display: inline-flex;
      flex: none;
      align-items: center;
      justify-content: center;
      width: var(--size, 40px);
      height: var(--size, 40px);
      border-radius: 50%;
      overflow: hidden;
      background: var(--lk-primary-50);
      color: var(--lk-primary-700);
      font-weight: 700;
      font-size: calc(var(--size, 40px) * 0.36);
      line-height: 1;
    }
    // A big avatar sits on the grouped background rather than on a card, so it needs a deeper tint.
    :host(.large) {
      background: var(--lk-primary-100);
    }
    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
  `,
  host: { 'aria-hidden': 'true', '[style.--size.px]': 'size()', '[class.large]': 'size() >= 64' },
  template: `
    @if (photo() && !broken()) {
      <img [src]="photo()" alt="" loading="lazy" (error)="broken.set(true)" />
    } @else {
      {{ monogram() }}
    }
  `,
})
export class MemberAvatar {
  readonly photo = input<string | null | undefined>(null);
  readonly firstName = input<string | null | undefined>('');
  readonly lastName = input<string | null | undefined>('');
  readonly size = input(40);
  protected readonly broken = signal(false);
  protected readonly monogram = computed(() => initials(this.firstName(), this.lastName()));
}
