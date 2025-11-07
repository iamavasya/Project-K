import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { Observable } from 'rxjs';
import { LeadershipDto } from '../../models/requests/leadership/leadershipDto';

@Injectable({
  providedIn: 'root'
})
export class LeadershipService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/leadership`;

  getLeadershipByTypeAndKey(type: 'kurin' | 'group' | 'kv', typeKey: string): Observable<LeadershipDto> {
    return this.http.get<LeadershipDto>(`${this.apiUrl}/type/${type}/${typeKey}`);
  }

  getLeadershipByKey(leadershipKey: string): Observable<LeadershipDto> {
    return this.http.get<LeadershipDto>(`${this.apiUrl}/${leadershipKey}`);
  }

  getLeadershipHistories(leadershipKey: string) {
    return this.http.get(`${this.apiUrl}/histories/${leadershipKey}`);
  }

  create(payload: LeadershipDto): Observable<LeadershipDto> {
    return this.http.post<LeadershipDto>(this.apiUrl, payload);
  }

  update(leadershipKey: string, payload: LeadershipDto): Observable<LeadershipDto> {
    return this.http.put<LeadershipDto>(`${this.apiUrl}/${leadershipKey}`, payload);
  }
}