import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { ClientCacheService } from '../client-cache/client-cache.service';
import { AGENDA_CACHE_PREFIX, ENTITY_CACHE_TTL_MS } from '../client-cache/cache-policy';
import {
  AgendaArchivePage,
  AgendaArchivePolicy,
  AgendaAssignTargets,
  AgendaBoardFilter,
  AgendaBoardResponse,
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

  /**
   * Dated items for the calendar within an optional [fromUtc, toUtc] window; with `includeSchedules`
   * also every event of a group marked «графік куреня» («Графіки гуртків»).
   */
  getCalendar(kurinKey: string, fromUtc?: string, toUtc?: string, includeSchedules = false): Observable<AgendaItemDto[]> {
    let params = new HttpParams().set('includeSchedules', String(includeSchedules));
    if (fromUtc) {
      params = params.set('fromUtc', fromUtc);
    }
    if (toUtc) {
      params = params.set('toUtc', toUtc);
    }
    const window = `${fromUtc ?? ''}:${toUtc ?? ''}:${includeSchedules ? 's' : ''}`;
    return this.cache.get(
      `${AGENDA_CACHE_PREFIX}calendar:${kurinKey}:${window}`,
      ENTITY_CACHE_TTL_MS,
      () => this.http.get<AgendaItemDto[]>(`${this.apiUrl}/${kurinKey}`, { params })
    );
  }

  /**
   * The board, filtered and paged per column on the server. Not cached: every search keystroke and
   * every «Завантажити ще» asks for a different slice.
   */
  getBoard(kurinKey: string, filter: AgendaBoardFilter = {}): Observable<AgendaBoardResponse> {
    return this.http.get<AgendaBoardResponse>(`${this.apiUrl}/${kurinKey}/board`, { params: this.toParams(filter) });
  }

  /** One item as the viewer sees it now. */
  getItem(agendaItemKey: string): Observable<AgendaItemDto> {
    return this.http.get<AgendaItemDto>(`${this.apiUrl}/item/${agendaItemKey}`);
  }

  getArchive(kurinKey: string, search: string | null, skip: number, take: number): Observable<AgendaArchivePage> {
    return this.http.get<AgendaArchivePage>(`${this.apiUrl}/${kurinKey}/archive`, { params: this.toParams({ search, skip, take }) });
  }

  setArchived(agendaItemKey: string, archived: boolean): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/${agendaItemKey}/archive`, { archived }).pipe(tap(() => this.invalidate()));
  }

  getArchivePolicy(kurinKey: string): Observable<AgendaArchivePolicy> {
    return this.http.get<AgendaArchivePolicy>(`${this.apiUrl}/${kurinKey}/archive-policy`);
  }

  setArchivePolicy(kurinKey: string, policy: AgendaArchivePolicy): Observable<AgendaArchivePolicy> {
    return this.http.put<AgendaArchivePolicy>(`${this.apiUrl}/${kurinKey}/archive-policy`, policy);
  }

  private toParams(values: object): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(values)) {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return params;
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

  /** One target, or — with `memberKey` — one person's part of a target done «кожному окремо». */
  changeTargetStatus(agendaItemKey: string, assignmentKey: string, status: AgendaItemStatus, memberKey: string | null = null): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/${agendaItemKey}/assignments/${assignmentKey}/status`, { status, memberKey })
      .pipe(tap(() => this.invalidate()));
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
