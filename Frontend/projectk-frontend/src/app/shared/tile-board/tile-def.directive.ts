import { Directive, TemplateRef, inject, input } from '@angular/core';
import { TileDefInput, TileSpan } from './tile-board.models';

@Directive({
  selector: '[appTileDef]'
})
export class TileDefDirective {
  readonly template = inject(TemplateRef<unknown>);

  readonly appTileDef = input.required<TileDefInput>();

  get key(): string {
    return this.appTileDef().key;
  }

  get span(): TileSpan {
    return this.appTileDef().span ?? 'full';
  }

  get pinned(): boolean {
    return this.appTileDef().pinned ?? false;
  }

  get label(): string {
    return this.appTileDef().label ?? this.appTileDef().key;
  }
}
