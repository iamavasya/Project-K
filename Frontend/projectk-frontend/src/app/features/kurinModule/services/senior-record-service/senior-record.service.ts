import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay, tap } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { MemberSeniorRecordDto, SeniorCatalogItem, UpsertSeniorRecordRequest } from '../../models/senior-record.dto';
import { ClientCacheService } from '../client-cache/client-cache.service';
import { MEMBER_CACHE_PREFIX } from '../client-cache/cache-policy';

/** The caller's own книжечка, and the catalogue it is written from. Read with the card. */
@Injectable({
  providedIn: 'root'
})
export class SeniorRecordService {
  private readonly apiUrl = `${environment.apiUrl}/me/senior-records`;
  private readonly http = inject(HttpClient);
  private readonly cache = inject(ClientCacheService);
  private catalog$?: Observable<SeniorCatalogItem[]>;

  catalog(): Observable<SeniorCatalogItem[]> {
    this.catalog$ ??= this.http.get<SeniorCatalogItem[]>(`${environment.apiUrl}/catalog/senior`).pipe(
      shareReplay({ bufferSize: 1, refCount: false })
    );
    return this.catalog$;
  }

  add(request: UpsertSeniorRecordRequest): Observable<MemberSeniorRecordDto> {
    return this.http.post<MemberSeniorRecordDto>(this.apiUrl, request).pipe(tap(() => this.invalidate()));
  }

  update(key: string, request: UpsertSeniorRecordRequest): Observable<MemberSeniorRecordDto> {
    return this.http.put<MemberSeniorRecordDto>(`${this.apiUrl}/${key}`, request).pipe(tap(() => this.invalidate()));
  }

  remove(key: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${key}`).pipe(tap(() => this.invalidate()));
  }

  private invalidate(): void {
    this.cache.invalidateByPrefix(MEMBER_CACHE_PREFIX);
  }
}
