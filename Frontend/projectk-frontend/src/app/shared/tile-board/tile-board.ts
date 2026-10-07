import { CdkDrag, CdkDragDrop, CdkDragHandle, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChildren,
  DestroyRef,
  effect,
  inject,
  input,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonModule } from '@openng/optimus-ui/button';
import { reconcileOrder } from './tile-order.function';
import { TileDefDirective } from './tile-def.directive';
import { TileDefinition, TileLayout } from './tile-board.models';
import { TileLayoutService } from './tile-layout.service';

/**
 * A board of tiles a person may arrange — and, where the board allows it, thin out. Order and hidden
 * tiles are kept apart: hiding a tile does not move the others, and bringing it back puts it where it
 * stood. Changes are written when the person leaves edit mode, not on every drag.
 */
@Component({
  selector: 'app-tile-board',
  imports: [NgTemplateOutlet, CdkDropList, CdkDrag, CdkDragHandle, ButtonModule],
  templateUrl: './tile-board.html',
  styleUrl: './tile-board.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TileBoardComponent {
  private readonly layoutService = inject(TileLayoutService);
  private readonly destroyRef = inject(DestroyRef);

  readonly boardKey = input.required<string>();
  readonly canReorder = input(true);
  /** Whether a tile may be taken off the board. A pinned tile never can. */
  readonly canHide = input(false);

  private readonly tileDefs = contentChildren(TileDefDirective);

  private readonly definitions = computed<TileDefinition[]>(() =>
    this.tileDefs().map((dir, index) => ({
      key: dir.key,
      span: dir.span,
      pinned: dir.pinned,
      label: dir.label,
      defaultOrder: index,
      template: dir.template
    }))
  );

  private readonly savedKeys = signal<string[] | null>(null);
  private readonly hiddenKeys = signal<readonly string[]>([]);

  readonly editMode = signal(false);

  readonly orderedTiles = computed<TileDefinition[]>(() => {
    const defs = this.definitions();
    const saved = this.savedKeys();
    return reconcileOrder(saved ?? [], defs);
  });

  /** What is on the board: the order, less what the person hid. */
  readonly visibleTiles = computed<TileDefinition[]>(() => {
    const hidden = new Set(this.canHide() ? this.hiddenKeys() : []);
    return this.orderedTiles().filter(tile => !hidden.has(tile.key));
  });

  /** Tiles the person hid that are still declared — a tile that no longer exists is not offered back. */
  readonly hiddenTiles = computed<TileDefinition[]>(() => {
    const hidden = new Set(this.hiddenKeys());
    return this.canHide() ? this.orderedTiles().filter(tile => hidden.has(tile.key)) : [];
  });

  private dirty = false;

  constructor() {
    effect(() => {
      const board = this.boardKey();
      const cached = this.layoutService.readCachedLayout(board);
      if (cached) {
        this.applyLayout(cached);
      }
      this.loadFromServer(board);
    });
  }

  private loadFromServer(board: string): void {
    this.layoutService
      .getLayout(board)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: layout => {
          if (layout) {
            this.applyLayout(layout);
          }
        },
        error: () => undefined
      });
  }

  private applyLayout(layout: TileLayout): void {
    this.savedKeys.set(layout.tileKeys);
    this.hiddenKeys.set(layout.hiddenTileKeys);
  }

  toggleEditMode(): void {
    if (this.editMode()) {
      this.exitEditMode();
    } else {
      this.editMode.set(true);
    }
  }

  private exitEditMode(): void {
    this.editMode.set(false);
    if (!this.dirty) {
      return;
    }
    this.dirty = false;
    this.persistLayout();
  }

  private persistLayout(): void {
    const keys = this.orderedTiles().map(tile => tile.key);
    this.savedKeys.set(keys);
    this.layoutService
      .saveLayout(this.boardKey(), { tileKeys: keys, hiddenTileKeys: [...this.hiddenKeys()] })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => undefined });
  }

  onDrop(event: CdkDragDrop<unknown>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }
    const next = [...this.visibleTiles()];
    moveItemInArray(next, event.previousIndex, event.currentIndex);
    this.commitVisibleOrder(next);
  }

  moveTile(index: number, direction: -1 | 1): void {
    const target = index + direction;
    const tiles = this.visibleTiles();
    if (target < 0 || target >= tiles.length) {
      return;
    }
    const next = [...tiles];
    moveItemInArray(next, index, target);
    this.commitVisibleOrder(next);
  }

  hideTile(tile: TileDefinition): void {
    if (!this.canHide() || tile.pinned || this.hiddenKeys().includes(tile.key)) {
      return;
    }
    this.dirty = true;
    this.hiddenKeys.set([...this.hiddenKeys(), tile.key]);
  }

  restoreTile(tile: TileDefinition): void {
    this.dirty = true;
    this.hiddenKeys.set(this.hiddenKeys().filter(key => key !== tile.key));
  }

  reset(): void {
    this.dirty = false;
    this.savedKeys.set(null);
    this.hiddenKeys.set([]);
    this.layoutService
      .resetLayout(this.boardKey())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => undefined });
  }

  /** The hidden tiles keep their old places: the new order is the visible one with them slotted back. */
  private commitVisibleOrder(visible: TileDefinition[]): void {
    this.dirty = true;
    const hidden = new Set(this.hiddenTiles().map(tile => tile.key));
    const full = this.orderedTiles();
    const merged: string[] = [];
    let next = 0;
    for (const tile of full) {
      merged.push(hidden.has(tile.key) ? tile.key : visible[next++].key);
    }
    this.savedKeys.set(merged);
  }

  spanClass(tile: TileDefinition): string {
    return `tile-slot--${tile.span}`;
  }

  ariaLabel(tile: TileDefinition, index: number, total: number): string {
    return `${tile.label}, позиція ${index + 1} з ${total}`;
  }

  isReorderable(tile: TileDefinition): boolean {
    return this.editMode() && this.canReorder() && !tile.pinned;
  }

  isHideable(tile: TileDefinition): boolean {
    return this.editMode() && this.canHide() && !tile.pinned;
  }
}
