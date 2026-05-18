import { Injectable } from '@angular/core';
import { Observable, catchError, shareReplay, throwError } from 'rxjs';

interface CacheEntry<T> {
  expiresAt: number;
  value$: Observable<T>;
}

@Injectable({
  providedIn: 'root'
})
export class ClientCacheService {
  private readonly entries = new Map<string, CacheEntry<unknown>>();

  get<T>(key: string, ttlMs: number, factory: () => Observable<T>): Observable<T> {
    const cached = this.entries.get(key) as CacheEntry<T> | undefined;
    const now = Date.now();

    if (cached && cached.expiresAt > now) {
      this.logDebug('hit', { key, expiresAt: cached.expiresAt });
      return cached.value$;
    }

    this.logDebug('miss', {
      key,
      reason: cached ? 'expired' : 'empty'
    });

    const value$ = factory().pipe(
      catchError(error => {
        this.entries.delete(key);
        return throwError(() => error);
      }),
      shareReplay({ bufferSize: 1, refCount: false })
    );

    this.entries.set(key, {
      expiresAt: now + ttlMs,
      value$
    });

    return value$;
  }

  invalidate(key: string): void {
    const invalidated = this.entries.delete(key);
    this.logDebug('invalidate', { key, invalidatedCount: invalidated ? 1 : 0 });
  }

  invalidateByPrefix(prefix: string): void {
    const matchingKeys = [...this.entries.keys()]
      .filter(key => key.startsWith(prefix));

    matchingKeys.forEach(key => this.entries.delete(key));
    this.logDebug('invalidate', { prefix, invalidatedCount: matchingKeys.length });
  }

  clear(): void {
    const invalidatedCount = this.entries.size;
    this.entries.clear();
    this.logDebug('invalidate', { prefix: '*', invalidatedCount });
  }

  private logDebug(event: 'hit' | 'miss' | 'invalidate', details: Record<string, unknown>): void {
    console.debug('[ClientCache]', event, details);
  }
}
