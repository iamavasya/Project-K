import { Injectable, inject } from '@angular/core';
import { Api } from '../../core/api';
import {
  AgendaAssignTargets,
  AgendaBoardFilter,
  AgendaBoardResponse,
  AgendaCategoryDto,
  AgendaItemDto,
  AgendaItemStatus,
  AgendaResponsesResponse,
  AgendaRsvpStatus,
  CreateAgendaItemRequest,
  UpdateAgendaItemRequest,
} from './agenda.models';

/** The web's AgendaService (kurinModule/services/agenda-service): the same endpoints and bodies. */
@Injectable({ providedIn: 'root' })
export class AgendaService {
  private readonly api = inject(Api);
  /** Who can be addressed changes rarely; asked once per kurin while the app is open. */
  private readonly targets = new Map<string, Promise<AgendaAssignTargets>>();

  /** Dated items in the window; with `includeSchedules` also every гурток's «графік куреня». */
  calendar(kurinKey: string, fromUtc: string, toUtc: string, includeSchedules: boolean): Promise<AgendaItemDto[]> {
    return this.api.get(`agenda/${kurinKey}`, { fromUtc, toUtc, includeSchedules });
  }

  board(kurinKey: string, filter: AgendaBoardFilter): Promise<AgendaBoardResponse> {
    return this.api.get(`agenda/${kurinKey}/board`, { ...filter });
  }

  item(agendaItemKey: string): Promise<AgendaItemDto> {
    return this.api.get(`agenda/item/${agendaItemKey}`);
  }

  assignTargets(kurinKey: string): Promise<AgendaAssignTargets> {
    let request = this.targets.get(kurinKey);
    if (!request) {
      request = this.api.get<AgendaAssignTargets>(`agenda/${kurinKey}/assign-targets`);
      request.catch(() => this.targets.delete(kurinKey));
      this.targets.set(kurinKey, request);
    }
    return request;
  }

  categories(kurinKey: string): Promise<AgendaCategoryDto[]> {
    return this.api.get(`agenda/${kurinKey}/categories`);
  }

  create(request: CreateAgendaItemRequest): Promise<string> {
    return this.api.post('agenda', request);
  }

  update(request: UpdateAgendaItemRequest): Promise<unknown> {
    return this.api.put(`agenda/${request.agendaItemKey}`, request);
  }

  /** The viewer's own part, or the shared targets they answer for (the board's move). */
  changeStatus(agendaItemKey: string, status: AgendaItemStatus): Promise<unknown> {
    return this.api.put(`agenda/${agendaItemKey}/status`, { status });
  }

  /** One target, or — with `memberKey` — one person's part of a target done «кожному окремо». */
  changeTargetStatus(
    agendaItemKey: string,
    assignmentKey: string,
    status: AgendaItemStatus,
    memberKey: string | null = null,
  ): Promise<unknown> {
    return this.api.put(`agenda/${agendaItemKey}/assignments/${assignmentKey}/status`, { status, memberKey });
  }

  setArchived(agendaItemKey: string, archived: boolean): Promise<unknown> {
    return this.api.put(`agenda/${agendaItemKey}/archive`, { archived });
  }

  /** The whole item; for a series, every occurrence (the web has no per-occurrence delete). */
  delete(agendaItemKey: string): Promise<unknown> {
    return this.api.delete(`agenda/${agendaItemKey}`);
  }

  /** The answers to one occurrence; `occurrenceStartUtc` is null for a one-off event. */
  responses(agendaItemKey: string, occurrenceStartUtc: string | null): Promise<AgendaResponsesResponse> {
    return this.api.get(`agenda/${agendaItemKey}/responses`, { occurrenceStartUtc });
  }

  respond(agendaItemKey: string, status: AgendaRsvpStatus, occurrenceStartUtc: string | null): Promise<AgendaResponsesResponse> {
    return this.api.put(`agenda/${agendaItemKey}/response`, { status, occurrenceStartUtc });
  }
}
