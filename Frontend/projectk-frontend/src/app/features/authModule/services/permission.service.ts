import { inject, Injectable } from '@angular/core';
import { AuthService } from './authService/auth.service';

@Injectable({
  providedIn: 'root'
})
export class PermissionService {
  private readonly authService = inject(AuthService);

  getRole(providedRole?: string | null): string {
    if (providedRole !== undefined && providedRole !== null) {
      return providedRole.trim().toLowerCase();
    }
    return (this.authService.getAuthStateValue?.()?.role ?? '').trim().toLowerCase();
  }

  isAdmin(role?: string | null): boolean {
    return this.getRole(role) === 'admin';
  }

  isManager(role?: string | null): boolean {
    return this.getRole(role) === 'manager';
  }

  isMentor(role?: string | null): boolean {
    return this.getRole(role) === 'mentor';
  }

  isReviewer(role?: string | null): boolean {
    const r = this.getRole(role);
    return r === 'mentor' || r === 'manager' || r === 'admin';
  }

  canManageGroups(role?: string | null): boolean {
    return this.isAdmin(role) || this.isManager(role);
  }

  canManageMembers(role?: string | null): boolean {
    const r = this.getRole(role);
    return r !== '' && r !== 'user';
  }

  canManageMentors(role?: string | null): boolean {
    return this.isAdmin(role) || this.isManager(role);
  }

  canManageWarnings(role?: string | null): boolean {
    return this.isReviewer(role);
  }

  canSetupLeadership(role?: string | null): boolean {
    const r = this.getRole(role);
    return r !== '' && r !== 'user';
  }

  canReviewSkills(role?: string | null): boolean {
    return this.isReviewer(role);
  }

  getRoleSeverity(role?: string | null): string {
    const normalized = this.getRole(role);
    switch (normalized) {
      case 'admin':
        return 'danger';
      case 'manager':
        return 'warning';
      case 'mentor':
        return 'success';
      default:
        return 'info';
    }
  }
}
