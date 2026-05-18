import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../../environments/environment';
import { KurinDto } from '../../models/kurinDto';
import { Observable } from 'rxjs/internal/Observable';
import { tap } from 'rxjs';
import { ClientCacheService } from '../client-cache/client-cache.service';
import { ENTITY_CACHE_TTL_MS, GROUP_CACHE_PREFIX, KURIN_CACHE_PREFIX, MEMBER_CACHE_PREFIX } from '../client-cache/cache-policy';

@Injectable({
  providedIn: 'root'
})
export class KurinService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(ClientCacheService);
  private readonly apiUrl = `${environment.apiUrl}/kurin`;

  getByKey(kurinKey: string): Observable<KurinDto> {
    return this.cache.get(
      `${KURIN_CACHE_PREFIX}by-key:${kurinKey}`,
      ENTITY_CACHE_TTL_MS,
      () => this.http.get<KurinDto>(`${this.apiUrl}/${kurinKey}`)
    );
  }

  getKurins(): Observable<KurinDto[]> {
    return this.cache.get(
      `${KURIN_CACHE_PREFIX}list`,
      ENTITY_CACHE_TTL_MS,
      () => this.http.get<KurinDto[]>(`${this.apiUrl}/kurins`)
    );
  }

  createKurin(kurin: KurinDto): Observable<KurinDto> {
    return this.http.post<KurinDto>(
      `${this.apiUrl}`,
      kurin.number,
      {
        headers: {
          'Content-Type': 'application/json'
        }
      }
    ).pipe(
      tap(() => this.invalidateKurinDataCache())
    );
  }

  updateKurin(kurin: KurinDto): Observable<KurinDto> {
    return this.http.put<KurinDto>(
      `${this.apiUrl}/${kurin.kurinKey}`,
      kurin.number,
      {
        headers: {
          'Content-Type': 'application/json'
        }
      }
    ).pipe(
      tap(() => this.invalidateKurinDataCache())
    );
  }

  deleteKurin(kurinKey: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${kurinKey}`).pipe(
      tap(() => this.invalidateKurinDataCache())
    );
  }

  private invalidateKurinDataCache(): void {
    this.cache.invalidateByPrefix(KURIN_CACHE_PREFIX);
    this.cache.invalidateByPrefix(GROUP_CACHE_PREFIX);
    this.cache.invalidateByPrefix(MEMBER_CACHE_PREFIX);
  }
}
