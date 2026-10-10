import { Component, input } from '@angular/core';

/**
 * The kurin's plaque: its number on the red square, as the web's kurin-number draws it (its small
 * size, from the my-kurins tile). Every measure follows `size`, so the square keeps its proportions.
 */
@Component({
  selector: 'app-kurin-plaque',
  host: { 'aria-hidden': 'true', '[style.--size.px]': 'size()', '[class.long]': 'number() > 99' },
  styles: `
    :host {
      display: inline-flex;
      flex: none;
      align-items: center;
      justify-content: center;
      box-sizing: border-box;
      width: var(--size, 40px);
      height: var(--size, 40px);
      background: #b30003;
      border: calc(var(--size, 40px) * 0.05) solid #ff0005;
      color: #ffffff;
      font-family: Arial, Helvetica, sans-serif;
      font-size: calc(var(--size, 40px) * 0.7);
      line-height: 1;
      user-select: none;
    }
    :host(.long) {
      font-size: calc(var(--size, 40px) * 0.55);
    }
  `,
  template: `{{ number() }}`,
})
export class KurinPlaque {
  readonly number = input.required<number>();
  readonly size = input(40);
}
