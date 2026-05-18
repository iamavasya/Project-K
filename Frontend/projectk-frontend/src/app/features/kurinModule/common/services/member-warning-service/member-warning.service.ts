import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../../../../environments/environment';
import { MemberWarningDto } from '../../models/memberWarningDto';
import { AssignMemberWarningRequestDto } from '../../models/requests/member/assignMemberWarningRequestDto';
import { ClientCacheService } from '../client-cache/client-cache.service';
import { ENTITY_CACHE_TTL_MS, MEMBER_CACHE_PREFIX, MEMBER_WARNING_CACHE_PREFIX } from '../client-cache/cache-policy';

@Injectable({
  providedIn: 'root'
})
export class MemberWarningService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(ClientCacheService);
  private readonly apiUrl = `${environment.apiUrl}/member`;

  getWarnings(memberKey: string): Observable<MemberWarningDto[]> {
    return this.cache.get(
      `${MEMBER_WARNING_CACHE_PREFIX}${memberKey}`,
      ENTITY_CACHE_TTL_MS,
      () => this.http.get<MemberWarningDto[]>(`${this.apiUrl}/${memberKey}/warnings`)
    );
  }

  assignWarning(memberKey: string, request: AssignMemberWarningRequestDto): Observable<MemberWarningDto> {
    return this.http.post<MemberWarningDto>(`${this.apiUrl}/${memberKey}/warnings`, request).pipe(
      tap(() => this.invalidateWarningData(memberKey))
    );
  }

  cancelWarning(memberKey: string, warningKey: string): Observable<MemberWarningDto> {
    return this.http.delete<MemberWarningDto>(`${this.apiUrl}/${memberKey}/warnings/${warningKey}`).pipe(
      tap(() => this.invalidateWarningData(memberKey))
    );
  }

  private invalidateWarningData(memberKey: string): void {
    this.cache.invalidate(`${MEMBER_WARNING_CACHE_PREFIX}${memberKey}`);
    this.cache.invalidateByPrefix(MEMBER_CACHE_PREFIX);
  }
}
