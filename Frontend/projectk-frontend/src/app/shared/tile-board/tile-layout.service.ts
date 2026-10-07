import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ClientCacheService } from '../../features/kurinModule/services/client-cache/client-cache.service';
import { ENTITY_CACHE_TTL_MS, LAYOUT_CACHE_PREFIX } from '../../features/kurinModule/services/client-cache/cache-policy';
import { TILE_LAYOUT_SCHEMA_VERSION, TileLayout } from './tile-board.models';
import { readStoredLayout, removeStoredLayout, writeStoredLayout } from './tile-layout-storage';
import { requestFeedback } from '../functions/request-feedback.function';

interface TileLayoutDto {
  boardKey: string;
  tileKeys: string[];
  hiddenTileKeys: string[];
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

  readCachedLayout(boardKey: string): TileLayout | null {
    return readStoredLayout(boardKey);
  }

  getLayout(boardKey: string): Observable<TileLayout | null> {
    return this.cache
      .get(
        `${LAYOUT_CACHE_PREFIX}all`,
        ENTITY_CACHE_TTL_MS,
        () => this.http.get<TileLayoutDto[]>(this.apiUrl)
      )
      .pipe(
        map(layouts => {
          const match = layouts.find(layout => layout.boardKey === boardKey);
          if (!match) {
            return null;
          }
          const layout: TileLayout = { tileKeys: match.tileKeys, hiddenTileKeys: match.hiddenTileKeys ?? [] };
          writeStoredLayout(boardKey, layout);
          return layout;
        })
      );
  }

  saveLayout(boardKey: string, layout: TileLayout): Observable<void> {
    writeStoredLayout(boardKey, layout);
    return this.http
      .put<TileLayoutDto>(`${this.apiUrl}/${boardKey}`, {
        tileKeys: layout.tileKeys,
        hiddenTileKeys: layout.hiddenTileKeys,
        schemaVersion: TILE_LAYOUT_SCHEMA_VERSION
      })
      .pipe(
        tap(() => this.cache.invalidateByPrefix(LAYOUT_CACHE_PREFIX)),
        map(() => undefined)
      );
  }

  resetLayout(boardKey: string): Observable<void> {
    removeStoredLayout(boardKey);
    return this.http.delete<void>(`${this.apiUrl}/${boardKey}`, { context: requestFeedback('errors') }).pipe(
      tap(() => this.cache.invalidateByPrefix(LAYOUT_CACHE_PREFIX)),
      map(() => undefined)
    );
  }
}
