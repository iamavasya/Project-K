import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../../../environments/environment';
import { BadgeCatalogItemDto } from '../../models/probes-and-badges/badgeCatalogItemDto';
import { BadgesMetadataDto } from '../../models/probes-and-badges/badgesMetadataDto';
import { ClientCacheService } from '../client-cache/client-cache.service';
import { BADGES_CATALOG_CACHE_PREFIX, CATALOG_CACHE_TTL_MS } from '../client-cache/cache-policy';

@Injectable({
  providedIn: 'root'
})
export class BadgesCatalogService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(ClientCacheService);
  private readonly apiUrl = `${environment.apiUrl}/catalog/badges`;

  getMetadata(): Observable<BadgesMetadataDto> {
    return this.cache.get(
      `${BADGES_CATALOG_CACHE_PREFIX}metadata`,
      CATALOG_CACHE_TTL_MS,
      () => this.http.get<BadgesMetadataDto>(`${this.apiUrl}/meta`)
    );
  }

  getAll(take = 200): Observable<BadgeCatalogItemDto[]> {
    const safeTake = Math.min(1000, Math.max(1, take));
    return this.cache.get(
      `${BADGES_CATALOG_CACHE_PREFIX}list:${safeTake}`,
      CATALOG_CACHE_TTL_MS,
      () => this.http.get<BadgeCatalogItemDto[]>(this.apiUrl, {
        params: {
          take: safeTake.toString()
        }
      })
    );
  }

  getById(badgeId: string): Observable<BadgeCatalogItemDto> {
    return this.cache.get(
      `${BADGES_CATALOG_CACHE_PREFIX}by-id:${badgeId}`,
      CATALOG_CACHE_TTL_MS,
      () => this.http.get<BadgeCatalogItemDto>(`${this.apiUrl}/${badgeId}`)
    );
  }
}
