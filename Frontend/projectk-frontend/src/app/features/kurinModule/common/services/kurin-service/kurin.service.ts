import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { environment } from '../../../../../../environments/environment';
import { KurinDto } from '../../models/kurinDto';
import { ApplyRosterRequest, RosterImportReport, RosterPreview } from '../../../import/import-model';
import { Observable } from 'rxjs/internal/Observable';
import { tap } from 'rxjs';
import { ClientCacheService } from '../client-cache/client-cache.service';
import { browserTimeZone } from '../../functions/browserTimeZone.function';
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

  /**
   * Звіт куреня в PDF. Разом із запитом їде часовий пояс браузера: години в документі мають бути
   * ті, що показує годинник читача, а сервер його поясу не знає — раніше там стояв UTC.
   */
  downloadReportPdf(kurinKey: string): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.apiUrl}/${kurinKey}/report/pdf`, {
      params: { timeZone: browserTimeZone() },
      observe: 'response',
      responseType: 'blob'
    });
  }

  /** Реєстр у .xlsx — саме тими колонками, які зараз видно на екрані. */
  exportRegistry(kurinKey: string, columns: string[]): Observable<HttpResponse<Blob>> {
    return this.http.post(`${this.apiUrl}/${kurinKey}/registry/export`, { columns }, {
      observe: 'response',
      responseType: 'blob'
    });
  }

  /** Читає завантажену таблицю й каже, на що вона схожа. Нічого не пише. */
  previewRoster(kurinKey: string, file: File): Observable<RosterPreview> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<RosterPreview>(`${this.apiUrl}/${kurinKey}/import/preview`, form);
  }

  /** Застосовує зіставлений склад — або, з `dryRun`, лише звітує, що б зробив. */
  importRoster(kurinKey: string, request: ApplyRosterRequest): Observable<RosterImportReport> {
    return this.http.post<RosterImportReport>(`${this.apiUrl}/${kurinKey}/import`, request).pipe(
      tap(report => {
        // Імпорт міняє склад, гуртки й розміщення разом — простіше скинути кеш цілком, ніж
        // перелічувати, що саме застаріло.
        if (!report.dryRun) {
          this.cache.clear();
        }
      })
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
      {
        number: kurin.number,
        stanytsia: kurin.stanytsia,
        regionOrCountry: kurin.regionOrCountry,
        namedAfter: kurin.namedAfter,
        description: kurin.description,
        profileVerificationEnabled: kurin.profileVerificationEnabled ?? false
      },
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
