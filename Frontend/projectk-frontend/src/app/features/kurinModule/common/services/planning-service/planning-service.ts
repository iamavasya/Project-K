import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../../environments/environment';
import { PlanningSessionDto } from '../../models/planningSessionDto';

@Injectable({ providedIn: 'root' })
export class PlanningService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/planning`;

  getSessions(kurinKey: string) {
    return this.http.get<PlanningSessionDto[]>(`${this.apiUrl}/${kurinKey}`);
  }

  getSessionByKey(sessionKey: string) {
    return this.http.get<PlanningSessionDto>(`${this.apiUrl}/session/${sessionKey}`);
  }

  createSession(payload: unknown) {
    return this.http.post(`${this.apiUrl}`, payload);
  }

  deleteSession(id: string) {
    return this.http.delete(`${this.apiUrl}/${id}`, {
      responseType: 'text'
    });
  }
}