/**
 * The web dashboard's tile board (shared/tile-board), read only: the order a person chose and the
 * tiles they hid on the web's Головна, applied to the phone's. No editing here.
 */

/** What the API keeps per board (`GET user/me/layouts`, TileLayoutDto). */
export interface TileLayoutDto {
  boardKey: string;
  tileKeys: string[];
  hiddenTileKeys?: string[] | null;
}

export interface TileLayout {
  tileKeys: string[];
  hiddenTileKeys: string[];
}

export const DASHBOARD_BOARD = 'dashboard';

/**
 * The web dashboard's tile keys (dashboard.html appTileDef) the phone's Головна has, in the phone's
 * own order, which is what shows when nothing is saved. «Мій профіль» and «Мої курені» live in
 * «Меню» on the phone.
 */
export type HomeTile = 'my-duties' | 'upcoming-events' | 'my-tasks' | 'my-probe' | 'my-skills' | 'my-score' | 'my-dues';
export const HOME_TILES: readonly HomeTile[] = ['my-duties', 'upcoming-events', 'my-tasks', 'my-probe', 'my-skills', 'my-score', 'my-dues'];

/** The board's saved layout among all of them; null when there is none (everything shows). */
export function boardLayout(layouts: TileLayoutDto[] | null | undefined, boardKey = DASHBOARD_BOARD): TileLayout | null {
  const match = (layouts ?? []).find((layout) => layout.boardKey === boardKey);
  return match ? { tileKeys: match.tileKeys ?? [], hiddenTileKeys: match.hiddenTileKeys ?? [] } : null;
}

/**
 * The tiles to show, in order (web reconcileOrder): the saved keys first as saved, then a tile the
 * layout does not know slotted in at its default place; less the hidden ones. `present` is what has
 * something to show now, in the default order.
 */
export function homeTiles(layout: TileLayout | null, present: readonly HomeTile[]): HomeTile[] {
  if (!layout) return [...present];
  const known = new Set<string>(present);
  const ordered: HomeTile[] = [];
  for (const key of layout.tileKeys) {
    if (known.has(key) && !ordered.includes(key as HomeTile)) ordered.push(key as HomeTile);
  }
  present.forEach((tile, index) => {
    if (!ordered.includes(tile)) ordered.splice(Math.min(index, ordered.length), 0, tile);
  });
  const hidden = new Set(layout.hiddenTileKeys);
  return ordered.filter((tile) => !hidden.has(tile));
}

const STORAGE_PREFIX = 'tile-layout:';

/**
 * The web keeps the last layout it read in localStorage (tile-layout-storage); the phone shares the
 * origin, so it starts from it too and no hidden tile flashes up while the API answers. Older web
 * builds wrote the order alone.
 */
export function readStoredLayout(boardKey = DASHBOARD_BOARD): TileLayout | null {
  try {
    const raw = localStorage.getItem(STORAGE_PREFIX + boardKey);
    if (!raw) return null;
    const parsed: unknown = JSON.parse(raw);
    if (isKeyList(parsed)) return { tileKeys: parsed, hiddenTileKeys: [] };
    if (parsed && typeof parsed === 'object') {
      const { tileKeys, hiddenTileKeys } = parsed as Partial<TileLayout>;
      if (isKeyList(tileKeys) && isKeyList(hiddenTileKeys)) return { tileKeys, hiddenTileKeys };
    }
    return null;
  } catch {
    return null;
  }
}

/** As the web does after a read, so either app starts from the newest layout; none clears it. */
export function writeStoredLayout(layout: TileLayout | null, boardKey = DASHBOARD_BOARD): void {
  try {
    if (layout) localStorage.setItem(STORAGE_PREFIX + boardKey, JSON.stringify(layout));
    else localStorage.removeItem(STORAGE_PREFIX + boardKey);
  } catch {
    // Storage off (private mode): the next start reads the API again.
  }
}

function isKeyList(value: unknown): value is string[] {
  return Array.isArray(value) && value.every((item) => typeof item === 'string');
}
