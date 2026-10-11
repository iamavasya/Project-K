import { Injectable, inject } from '@angular/core';
import { Api } from '../../core/api';
import { CreatePlanningRequest, PlanningSessionDto } from './planning.models';

/** The web's PlanningService (kurinModule/services/planning-service): the same endpoints and bodies. */
@Injectable({ providedIn: 'root' })
export class PlanningService {
  private readonly api = inject(Api);

  sessions(kurinKey: string): Promise<PlanningSessionDto[]> {
    return this.api.get(`planning/${kurinKey}`);
  }

  session(planningSessionKey: string): Promise<PlanningSessionDto> {
    return this.api.get(`planning/session/${planningSessionKey}`);
  }

  /** Saves the session and works out its dates at once; answers with the new key. */
  create(request: CreatePlanningRequest): Promise<string> {
    return this.api.post('planning', request);
  }

  /** The API answers with a line of text, as the web reads it. */
  delete(planningSessionKey: string): Promise<unknown> {
    return this.api.deleteText(`planning/${planningSessionKey}`);
  }
}
