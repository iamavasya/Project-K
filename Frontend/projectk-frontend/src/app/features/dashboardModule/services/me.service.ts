import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AgendaRsvpStatus } from '../../kurinModule/models/agenda';
import { MyDuesDto, MyEventDto, MyGrowthDto, MyScoreDto, MyTaskDto } from '../models/me.dto';

/**
 * What the dashboard reads about the person behind the token, across every kurin. Not cached: a
 * dashboard is opened to see what is new, and these reads are small.
 */
@Injectable({ providedIn: 'root' })
export class MeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/me`;

  getEvents(days = 14): Observable<MyEventDto[]> {
    return this.http.get<MyEventDto[]>(`${this.apiUrl}/events`, { params: new HttpParams().set('days', String(days)) });
  }

  setEventResponse(agendaItemKey: string, status: AgendaRsvpStatus): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/events/${agendaItemKey}/response`, { status });
  }

  getTasks(): Observable<MyTaskDto[]> {
    return this.http.get<MyTaskDto[]>(`${this.apiUrl}/tasks`);
  }

  getGrowth(): Observable<MyGrowthDto> {
    return this.http.get<MyGrowthDto>(`${this.apiUrl}/growth`);
  }

  getDues(): Observable<MyDuesDto[]> {
    return this.http.get<MyDuesDto[]>(`${this.apiUrl}/dues`);
  }

  getScore(): Observable<MyScoreDto[]> {
    return this.http.get<MyScoreDto[]>(`${this.apiUrl}/score`);
  }
}
