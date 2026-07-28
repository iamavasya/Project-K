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
import { ButtonModule } from 'primeng/button';
import { reconcileOrder } from './tile-order.function';
import { TileDefDirective } from './tile-def.directive';
import { TileDefinition } from './tile-board.models';
import { TileLayoutService } from './tile-layout.service';

@Component({
  selector: 'app-tile-board',
  imports: [NgTemplateOutlet, CdkDropList, CdkDrag, CdkDragHandle, ButtonModule],
  templateUrl: './tile-board.component.html',
  styleUrl: './tile-board.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TileBoardComponent {
  private readonly layoutService = inject(TileLayoutService);
  private readonly destroyRef = inject(DestroyRef);

  readonly boardKey = input.required<string>();
  readonly canReorder = input(true);

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

  readonly editMode = signal(false);

  readonly orderedTiles = computed<TileDefinition[]>(() => {
    const defs = this.definitions();
    const saved = this.savedKeys();
    return reconcileOrder(saved ?? [], defs);
  });

  private dirty = false;

  constructor() {
    effect(() => {
      const board = this.boardKey();
      const cached = this.layoutService.readCachedOrder(board);
      if (cached) {
        this.savedKeys.set(cached);
      }
      this.loadFromServer(board);
    });
  }

  private loadFromServer(board: string): void {
    this.layoutService
      .getOrder(board)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: keys => {
          if (keys) {
            this.savedKeys.set(keys);
          }
        },
        error: () => undefined
      });
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
    this.persistCurrentOrder();
  }

  private persistCurrentOrder(): void {
    const keys = this.orderedTiles().map(tile => tile.key);
    this.savedKeys.set(keys);
    this.layoutService
      .saveOrder(this.boardKey(), keys)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => undefined });
  }

  onDrop(event: CdkDragDrop<unknown>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }
    const next = [...this.orderedTiles()];
    moveItemInArray(next, event.previousIndex, event.currentIndex);
    this.commitOrder(next);
  }

  moveTile(index: number, direction: -1 | 1): void {
    const target = index + direction;
    const tiles = this.orderedTiles();
    if (target < 0 || target >= tiles.length) {
      return;
    }
    const next = [...tiles];
    moveItemInArray(next, index, target);
    this.commitOrder(next);
  }

  reset(): void {
    this.dirty = false;
    this.savedKeys.set(null);
    this.layoutService
      .resetOrder(this.boardKey())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => undefined });
  }

  private commitOrder(tiles: TileDefinition[]): void {
    this.dirty = true;
    this.savedKeys.set(tiles.map(tile => tile.key));
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
}
