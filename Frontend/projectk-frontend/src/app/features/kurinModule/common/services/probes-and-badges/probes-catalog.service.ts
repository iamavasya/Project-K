import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../../../environments/environment';
import { GroupedProbeDto } from '../../models/probes-and-badges/groupedProbeDto';
import { ProbeSummaryDto } from '../../models/probes-and-badges/probeSummaryDto';
import { ClientCacheService } from '../client-cache/client-cache.service';
import { CATALOG_CACHE_TTL_MS, PROBES_CATALOG_CACHE_PREFIX } from '../client-cache/cache-policy';

@Injectable({
  providedIn: 'root'
})
export class ProbesCatalogService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(ClientCacheService);
  private readonly apiUrl = `${environment.apiUrl}/catalog/probes`;

  getAll(): Observable<ProbeSummaryDto[]> {
    return this.cache.get(
      `${PROBES_CATALOG_CACHE_PREFIX}list`,
      CATALOG_CACHE_TTL_MS,
      () => this.http.get<ProbeSummaryDto[]>(this.apiUrl)
    );
  }

  getGroupedById(probeId: string): Observable<GroupedProbeDto> {
    return this.cache.get(
      `${PROBES_CATALOG_CACHE_PREFIX}grouped:${probeId}`,
      CATALOG_CACHE_TTL_MS,
      () => this.http.get<GroupedProbeDto>(`${this.apiUrl}/${probeId}/grouped`)
    );
  }
}
