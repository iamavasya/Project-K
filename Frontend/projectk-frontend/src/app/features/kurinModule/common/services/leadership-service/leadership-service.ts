import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LeadershipService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/leadership`;

  getLeadershipByTypeAndKey(type: 'kurin' | 'group' | 'kv', typeKey: string) {
    return this.http.get(`${this.apiUrl}/${type}/${typeKey}`);
  }

  createLeadership(type: 'kurin' | 'group' | 'kv', typeKey: string, data: any) {}
}