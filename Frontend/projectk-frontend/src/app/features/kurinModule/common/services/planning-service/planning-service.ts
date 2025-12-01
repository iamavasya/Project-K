import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { PlanningSessionDto } from '../../models/planningSessionDto';
import { MemberLookupDto } from '../../models/requests/member/memberLookupDto';

@Injectable({ providedIn: 'root' })
export class PlanningService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/planning`;

  // Отримати всі сесії куреня
  getSessions(kurinKey: string) {
    return this.http.get<PlanningSessionDto[]>(`${this.apiUrl}/${kurinKey}`);
  }

  getSessionByKey(sessionKey: string) {
    return this.http.get<PlanningSessionDto>(`${this.apiUrl}/session/${sessionKey}`);
  }

  // Створити сесію
  createSession(payload: any) {
    return this.http.post(`${this.apiUrl}`, payload);
  }

  deleteSession(id: string) {
    return this.http.delete(`${this.apiUrl}/${id}`, {
      responseType: 'text'
    });
  }
}