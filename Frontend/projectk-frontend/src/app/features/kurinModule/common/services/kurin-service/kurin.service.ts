import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { KurinDto } from '../../models/kurinDto';
import { Observable } from 'rxjs/internal/Observable';

@Injectable({
  providedIn: 'root'
})
export class KurinService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/kurin`;

  getByKey(kurinKey: string): Observable<KurinDto> {
    return this.http.get<KurinDto>(`${this.apiUrl}/${kurinKey}`);
  }

  getKurins(): Observable<KurinDto[]> {
    return this.http.get<KurinDto[]>(`${this.apiUrl}/kurins`);
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
    );
  }

  deleteKurin(kurinKey: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${kurinKey}`);
  }
}
