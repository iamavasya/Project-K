const STORAGE_PREFIX = 'tile-layout:';

export function readStoredOrder(boardKey: string): string[] | null {
  try {
    const raw = localStorage.getItem(STORAGE_PREFIX + boardKey);
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) && parsed.every(item => typeof item === 'string') ? parsed : null;
  } catch {
    return null;
  }
}

export function writeStoredOrder(boardKey: string, tileKeys: string[]): void {
  try {
    localStorage.setItem(STORAGE_PREFIX + boardKey, JSON.stringify(tileKeys));
  } catch {
    return;
  }
}

export function removeStoredOrder(boardKey: string): void {
  try {
    localStorage.removeItem(STORAGE_PREFIX + boardKey);
  } catch {
    return;
  }
}

export function clearTileLayoutStorage(): void {
  try {
    const keysToRemove: string[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith(STORAGE_PREFIX)) {
        keysToRemove.push(key);
      }
    }
    keysToRemove.forEach(key => localStorage.removeItem(key));
  } catch {
    return;
  }
}
