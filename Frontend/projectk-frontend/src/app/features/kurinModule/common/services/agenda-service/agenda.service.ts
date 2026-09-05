import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../../../../environments/environment';
import { ClientCacheService } from '../client-cache/client-cache.service';
import { AGENDA_CACHE_PREFIX, ENTITY_CACHE_TTL_MS } from '../client-cache/cache-policy';
import {
  AgendaAssignTargets,
  AgendaCategoryDto,
  AgendaItemDto,
  AgendaItemStatus,
  AgendaResponsesResponse,
  AgendaRsvpStatus,
  CreateAgendaItemRequest,
  UpdateAgendaItemRequest,
  UpsertAgendaCategoryRequest
} from '../../models/agenda';

@Injectable({ providedIn: 'root' })
export class AgendaService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(ClientCacheService);
  private readonly apiUrl = `${environment.apiUrl}/agenda`;

  /** Dated items for the calendar within an optional [fromUtc, toUtc] window. */
  getCalendar(kurinKey: string, fromUtc?: string, toUtc?: string): Observable<AgendaItemDto[]> {
    let params = new HttpParams();
    if (fromUtc) {
      params = params.set('fromUtc', fromUtc);
    }
    if (toUtc) {
      params = params.set('toUtc', toUtc);
    }
    const window = `${fromUtc ?? ''}:${toUtc ?? ''}`;
    return this.cache.get(
      `${AGENDA_CACHE_PREFIX}calendar:${kurinKey}:${window}`,
      ENTITY_CACHE_TTL_MS,
      () => this.http.get<AgendaItemDto[]>(`${this.apiUrl}/${kurinKey}`, { params })
    );
  }

  /** Every task the current user may see, grouped client-side into board columns. */
  getBoard(kurinKey: string): Observable<AgendaItemDto[]> {
    return this.cache.get(
      `${AGENDA_CACHE_PREFIX}board:${kurinKey}`,
      ENTITY_CACHE_TTL_MS,
      () => this.http.get<AgendaItemDto[]>(`${this.apiUrl}/${kurinKey}/board`)
    );
  }

  getAssignTargets(kurinKey: string): Observable<AgendaAssignTargets> {
    return this.cache.get(
      `${AGENDA_CACHE_PREFIX}targets:${kurinKey}`,
      ENTITY_CACHE_TTL_MS,
      () => this.http.get<AgendaAssignTargets>(`${this.apiUrl}/${kurinKey}/assign-targets`)
    );
  }

  create(request: CreateAgendaItemRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, request).pipe(tap(() => this.invalidate()));
  }

  update(request: UpdateAgendaItemRequest): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/${request.agendaItemKey}`, request).pipe(tap(() => this.invalidate()));
  }

  changeStatus(agendaItemKey: string, status: AgendaItemStatus): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/${agendaItemKey}/status`, { status }).pipe(tap(() => this.invalidate()));
  }

  delete(agendaItemKey: string): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/${agendaItemKey}`, { responseType: 'text' }).pipe(tap(() => this.invalidate()));
  }

  // ---- Event groups (categories) ----

  /** Active event groups for the item dialog picker. */
  getCategories(kurinKey: string): Observable<AgendaCategoryDto[]> {
    return this.cache.get(
      `${AGENDA_CACHE_PREFIX}categories:${kurinKey}`,
      ENTITY_CACHE_TTL_MS,
      () => this.http.get<AgendaCategoryDto[]>(`${this.apiUrl}/${kurinKey}/categories`)
    );
  }

  /** All event groups incl. archived — for the Зв'язковий management page (not cached). */
  getCategoriesForManagement(kurinKey: string): Observable<AgendaCategoryDto[]> {
    return this.http.get<AgendaCategoryDto[]>(`${this.apiUrl}/${kurinKey}/categories/manage`);
  }

  upsertCategory(request: UpsertAgendaCategoryRequest): Observable<AgendaCategoryDto> {
    const call$ = request.agendaCategoryKey
      ? this.http.put<AgendaCategoryDto>(`${this.apiUrl}/categories/${request.agendaCategoryKey}`, request)
      : this.http.post<AgendaCategoryDto>(`${this.apiUrl}/categories`, request);
    return call$.pipe(tap(() => this.invalidate()));
  }

  deleteCategory(kurinKey: string, categoryKey: string): Observable<unknown> {
    return this.http
      .delete(`${this.apiUrl}/${kurinKey}/categories/${categoryKey}`, { responseType: 'text' })
      .pipe(tap(() => this.invalidate()));
  }

  // ---- RSVP ----

  getResponses(agendaItemKey: string): Observable<AgendaResponsesResponse> {
    return this.http.get<AgendaResponsesResponse>(`${this.apiUrl}/${agendaItemKey}/responses`);
  }

  setResponse(agendaItemKey: string, status: AgendaRsvpStatus): Observable<AgendaResponsesResponse> {
    return this.http
      .put<AgendaResponsesResponse>(`${this.apiUrl}/${agendaItemKey}/response`, { status })
      .pipe(tap(() => this.invalidate()));
  }

  private invalidate(): void {
    this.cache.invalidateByPrefix(AGENDA_CACHE_PREFIX);
  }
}
