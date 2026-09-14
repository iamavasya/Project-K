import { inject, Injectable } from '@angular/core';
import { AuthService } from '../auth-service/auth.service';

/** The three провід bodies a leadership record belongs to. */
export type LeadershipScope = 'kv' | 'kurin' | 'group';

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
  canManageWholeKurin(): boolean {
    return this.isAdmin() || this.has('Group:Manage:KurinWide');
  }

  /** Runs a гурток: Виховник and above. Провід offices are not included — they lead, they do not moderate. */
  canLeadGroups(): boolean {
    return this.has('Group:Update');
  }

  isReviewer(): boolean {
    return this.canLeadGroups() || this.canManageWholeKurin();
  }

  canManageGroups(): boolean {
    return this.canManageWholeKurin();
  }

  canManageMembers(): boolean {
    return this.isReviewer();
  }

  canManageMentors(): boolean {
    return this.canManageWholeKurin();
  }

  canManageWarnings(): boolean {
    return this.isReviewer();
  }

  /**
   * Who seats a провід, per body, the way the backend's AssignableOffices has it: Звʼязковий and
   * admin every one; Курінний only the kurin провід; Гуртковий only his гурток's. Checking
   * `Leadership:Update` alone let a Гуртковий open the kurin провід form and meet a 403.
   */
  canSetupLeadership(type: LeadershipScope): boolean {
    if (this.isAdmin() || this.has('Leadership:Manage:KurinWide')) {
      return true;
    }
    switch (type) {
      case 'kv':
        return false;
      case 'kurin':
        return this.has('Leadership:Update:KurinWide');
      case 'group':
        return this.has('Leadership:Update:OwnGroups');
    }
  }

  canReviewSkills(): boolean {
    return this.isReviewer();
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
    if (this.canManageWholeKurin()) {
      return 'warn';
    }
    if (this.canLeadGroups()) {
      return 'success';
    }
    return 'info';
  }
}
