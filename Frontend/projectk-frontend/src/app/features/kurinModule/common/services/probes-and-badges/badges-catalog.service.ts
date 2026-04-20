import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../../../environments/environment';
import { BadgeCatalogItemDto } from '../../models/probes-and-badges/badgeCatalogItemDto';
import { BadgesMetadataDto } from '../../models/probes-and-badges/badgesMetadataDto';

@Injectable({
  providedIn: 'root'
})
export class BadgesCatalogService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/catalog/badges`;

  getMetadata(): Observable<BadgesMetadataDto> {
    return this.http.get<BadgesMetadataDto>(`${this.apiUrl}/meta`);
  }

  getAll(take = 200): Observable<BadgeCatalogItemDto[]> {
    const safeTake = Math.min(1000, Math.max(1, take));
    return this.http.get<BadgeCatalogItemDto[]>(this.apiUrl, {
      params: {
        take: safeTake.toString()
      }
    });
  }

  getById(badgeId: string): Observable<BadgeCatalogItemDto> {
    return this.http.get<BadgeCatalogItemDto>(`${this.apiUrl}/${badgeId}`);
  }
}