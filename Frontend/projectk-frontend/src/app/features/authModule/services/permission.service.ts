import { inject, Injectable } from '@angular/core';
import { AuthService } from './authService/auth.service';

/**
 * Central UI gate. Every predicate is derived from the current user's backend permission strings
 * (format `Resource:Action:Scope`, e.g. `Group:Manage:KurinWide`), so nothing here inspects role
 * names. The backend enforces the same permissions with scope on every request.
 */
@Injectable({
  providedIn: 'root'
})
export class PermissionService {
  private readonly authService = inject(AuthService);

  private permissions(): string[] {
    return this.authService.getAuthStateValue?.()?.permissions ?? [];
  }

  private has(prefix: string): boolean {
    return this.permissions().some(permission => permission.startsWith(prefix));
  }

  isAdmin(): boolean {
    return this.authService.getAuthStateValue?.()?.isAdmin ?? false;
  }

  /** Whole-kurin managers: Зв'язковий and admin. */
  isManager(): boolean {
    return this.isAdmin() || this.has('Group:Manage:KurinWide');
  }

  /** Runs a гурток: Виховник and above. Провід offices are not included — they lead, they do not moderate. */
  isMentor(): boolean {
    return this.has('Group:Update');
  }

  isReviewer(): boolean {
    return this.isMentor() || this.isManager();
  }

  canManageGroups(): boolean {
    return this.isManager();
  }

  canManageMembers(): boolean {
    return this.isReviewer();
  }

  canManageMentors(): boolean {
    return this.isManager();
  }

  canManageWarnings(): boolean {
    return this.isReviewer();
  }

  // Зв'язковий manages every провід; Курінний and Гуртковий seat the offices below them, which the
  // backend narrows to their own body.
  canSetupLeadership(): boolean {
    return this.isAdmin() || this.has('Leadership:Manage:KurinWide') || this.has('Leadership:Update');
  }

  canReviewSkills(): boolean {
    return this.isReviewer();
  }

  /** Kurin-wide planning control (Зв'язковий, admin) — the destructive row actions. */
  canManagePlanning(): boolean {
    return this.isAdmin() || this.has('PlanningSession:Manage:KurinWide');
  }

  /** Opening a planning session: the whole провід, each within their own scope. */
  canCreatePlanning(): boolean {
    return this.isAdmin() || this.has('PlanningSession:Create');
  }

  // The whole провід raises agenda items; the backend decides who may edit one afterwards
  // (its author, or the Виховник of a гурток it targets).
  canManageAgenda(): boolean {
    return this.isAdmin() || this.has('AgendaItem:Create');
  }

  canManageKurinSettings(): boolean {
    return this.isAdmin() || this.has('Kurin:Update:KurinWide');
  }

  getRoleSeverity(): string {
    if (this.isAdmin()) {
      return 'danger';
    }
    if (this.isManager()) {
      return 'warning';
    }
    if (this.isMentor()) {
      return 'success';
    }
    return 'info';
  }
}
