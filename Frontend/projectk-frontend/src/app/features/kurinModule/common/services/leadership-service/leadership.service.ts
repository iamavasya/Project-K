import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../../../environments/environment';
import { Observable, tap } from 'rxjs';
import { LeadershipDto } from '../../models/requests/leadership/leadershipDto';
import { ClientCacheService } from '../client-cache/client-cache.service';
import { ENTITY_CACHE_TTL_MS, LEADERSHIP_CACHE_PREFIX, MEMBER_CACHE_PREFIX } from '../client-cache/cache-policy';

@Injectable({
  providedIn: 'root'
})
export class LeadershipService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(ClientCacheService);
  private readonly apiUrl = `${environment.apiUrl}/leadership`;

  getLeadershipByTypeAndKey(type: 'kurin' | 'group' | 'kv', typeKey: string): Observable<LeadershipDto> {
    return this.cache.get(
      `${LEADERSHIP_CACHE_PREFIX}type:${type}:${typeKey}`,
      ENTITY_CACHE_TTL_MS,
      () => this.http.get<LeadershipDto>(`${this.apiUrl}/type/${type}/${typeKey}`)
    );
  }

  getLeadershipByKey(leadershipKey: string): Observable<LeadershipDto> {
    return this.cache.get(
      `${LEADERSHIP_CACHE_PREFIX}by-key:${leadershipKey}`,
      ENTITY_CACHE_TTL_MS,
      () => this.http.get<LeadershipDto>(`${this.apiUrl}/${leadershipKey}`)
    );
  }

  getLeadershipHistories(leadershipKey: string) {
    return this.cache.get(
      `${LEADERSHIP_CACHE_PREFIX}histories:${leadershipKey}`,
      ENTITY_CACHE_TTL_MS,
      () => this.http.get(`${this.apiUrl}/histories/${leadershipKey}`)
    );
  }

  create(payload: LeadershipDto): Observable<LeadershipDto> {
    return this.http.post<LeadershipDto>(this.apiUrl, payload).pipe(
      tap(() => this.invalidateLeadershipData())
    );
  }

  update(leadershipKey: string, payload: LeadershipDto): Observable<LeadershipDto> {
    return this.http.put<LeadershipDto>(`${this.apiUrl}/${leadershipKey}`, payload).pipe(
      tap(() => this.invalidateLeadershipData())
    );
  }

  private invalidateLeadershipData(): void {
    this.cache.invalidateByPrefix(LEADERSHIP_CACHE_PREFIX);
    this.cache.invalidateByPrefix(MEMBER_CACHE_PREFIX);
  }
}
