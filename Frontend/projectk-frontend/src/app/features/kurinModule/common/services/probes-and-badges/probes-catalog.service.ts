import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { GroupedProbeDto } from '../../models/probes-and-badges/groupedProbeDto';
import { ProbeSummaryDto } from '../../models/probes-and-badges/probeSummaryDto';

@Injectable({
  providedIn: 'root'
})
export class ProbesCatalogService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/catalog/probes`;

  getAll(): Observable<ProbeSummaryDto[]> {
    return this.http.get<ProbeSummaryDto[]>(this.apiUrl);
  }

  getGroupedById(probeId: string): Observable<GroupedProbeDto> {
    return this.http.get<GroupedProbeDto>(`${this.apiUrl}/${probeId}/grouped`);
  }
}