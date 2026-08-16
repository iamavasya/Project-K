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

  /** Whole-kurin managers: Зв'язковий, Курінний, admin. */
  isManager(): boolean {
    return this.isAdmin() || this.has('Group:Manage:KurinWide');
  }

  /** Group leaders (гуртковий) and above. */
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

  canSetupLeadership(): boolean {
    return this.isAdmin() || this.has('Leadership:Manage:KurinWide');
  }

  canReviewSkills(): boolean {
    return this.isReviewer();
  }

  canManagePlanning(): boolean {
    return this.isAdmin() || this.has('PlanningSession:Manage:KurinWide');
  }

  // Anyone above a bare member (гуртковий leaders, kurin managers, admin) may create/assign agenda
  // items; the backend narrows a group leader to their led groups per target.
  canManageAgenda(): boolean {
    return this.isReviewer();
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
