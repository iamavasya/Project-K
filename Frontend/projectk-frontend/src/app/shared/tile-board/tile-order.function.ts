import { TileDefinition } from './tile-board.models';

export function reconcileOrder(savedKeys: readonly string[], defs: readonly TileDefinition[]): TileDefinition[] {
  const byKey = new Map(defs.map(def => [def.key, def]));

  const seen = new Set<string>();
  const ordered: TileDefinition[] = [];
  for (const key of savedKeys) {
    const def = byKey.get(key);
    if (def && !seen.has(key)) {
      ordered.push(def);
      seen.add(key);
    }
  }

  const missing = defs
    .filter(def => !seen.has(def.key))
    .sort((a, b) => a.defaultOrder - b.defaultOrder);

  for (const def of missing) {
    const insertAt = clamp(def.defaultOrder, 0, ordered.length);
    ordered.splice(insertAt, 0, def);
  }

  const pinned = ordered.filter(def => def.pinned).sort((a, b) => a.defaultOrder - b.defaultOrder);
  const rest = ordered.filter(def => !def.pinned);
  return [...pinned, ...rest];
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}
