import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ClientCacheService } from '../../features/kurinModule/common/services/client-cache/client-cache.service';
import { ENTITY_CACHE_TTL_MS, LAYOUT_CACHE_PREFIX } from '../../features/kurinModule/common/services/client-cache/cache-policy';
import { TILE_LAYOUT_SCHEMA_VERSION } from './tile-board.models';
import { readStoredOrder, removeStoredOrder, writeStoredOrder } from './tile-layout-storage';

interface TileLayoutDto {
  boardKey: string;
  tileKeys: string[];
  schemaVersion: number;
  updatedAtUtc: string;
}

@Injectable({
  providedIn: 'root'
})
export class TileLayoutService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(ClientCacheService);
  private readonly apiUrl = `${environment.apiUrl}/user/me/layouts`;

  readCachedOrder(boardKey: string): string[] | null {
    return readStoredOrder(boardKey);
  }

  getOrder(boardKey: string): Observable<string[] | null> {
    return this.cache
      .get(
        `${LAYOUT_CACHE_PREFIX}all`,
        ENTITY_CACHE_TTL_MS,
        () => this.http.get<TileLayoutDto[]>(this.apiUrl)
      )
      .pipe(
        map(layouts => {
          const match = layouts.find(layout => layout.boardKey === boardKey);
          const keys = match ? match.tileKeys : null;
          if (keys) {
            writeStoredOrder(boardKey, keys);
          }
          return keys;
        })
      );
  }

  saveOrder(boardKey: string, tileKeys: string[]): Observable<void> {
    writeStoredOrder(boardKey, tileKeys);
    return this.http
      .put<TileLayoutDto>(`${this.apiUrl}/${boardKey}`, {
        tileKeys,
        schemaVersion: TILE_LAYOUT_SCHEMA_VERSION
      })
      .pipe(
        tap(() => this.cache.invalidateByPrefix(LAYOUT_CACHE_PREFIX)),
        map(() => undefined)
      );
  }

  resetOrder(boardKey: string): Observable<void> {
    removeStoredOrder(boardKey);
    return this.http.delete<void>(`${this.apiUrl}/${boardKey}`).pipe(
      tap(() => this.cache.invalidateByPrefix(LAYOUT_CACHE_PREFIX)),
      map(() => undefined)
    );
  }
}
