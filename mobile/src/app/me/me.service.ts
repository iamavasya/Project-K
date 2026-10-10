import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { apiUrl } from '../runtime-config';
import {
  AgendaItemStatus,
  AgendaRsvpStatus,
  MemberDto,
  MyDuesDto,
  MyEventDto,
  MyGrowthDto,
  MyScoreDto,
  MyTaskDto,
} from './me.models';

/** What the phone reads about the signed-in person: the same endpoints as the web dashboard. */
@Injectable({ providedIn: 'root' })
export class MeService {
  private readonly http = inject(HttpClient);
  private readonly api = apiUrl();

  events(days = 14): Promise<MyEventDto[]> {
    return this.get(`me/events?days=${days}`);
  }

  tasks(): Promise<MyTaskDto[]> {
    return this.get('me/tasks');
  }

  growth(): Promise<MyGrowthDto> {
    return this.get('me/growth');
  }

  dues(): Promise<MyDuesDto[]> {
    return this.get('me/dues');
  }

  score(): Promise<MyScoreDto[]> {
    return this.get('me/score');
  }

  member(memberKey: string): Promise<MemberDto> {
    return this.get(`member/${memberKey}`);
  }

  /** The answer to one occurrence; a series is answered per occurrence, by its start. */
  respond(event: MyEventDto, status: AgendaRsvpStatus): Promise<unknown> {
    const occurrenceStartUtc = event.isRecurring ? event.startUtc : null;
    return firstValueFrom(
      this.http.put(`${this.api}/me/events/${event.agendaItemKey}/response`, { status, occurrenceStartUtc }),
    );
  }

  /** Through the board's own endpoint, as the web dashboard does. */
  moveTask(task: MyTaskDto, status: AgendaItemStatus): Promise<unknown> {
    return firstValueFrom(this.http.put(`${this.api}/agenda/${task.agendaItemKey}/status`, { status }));
  }

  private get<T>(path: string): Promise<T> {
    return firstValueFrom(this.http.get<T>(`${this.api}/${path}`));
  }
}
