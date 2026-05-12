import { inject, Injectable } from '@angular/core';
import { AuthService } from './authService/auth.service';

@Injectable({
  providedIn: 'root'
})
export class PermissionService {
  private readonly authService = inject(AuthService);

  getRole(): string {
    return (this.authService.getAuthStateValue()?.role ?? '').trim().toLowerCase();
  }

  isAdmin(): boolean {
    return this.getRole() === 'admin';
  }

  isManager(): boolean {
    return this.getRole() === 'manager';
  }

  isMentor(): boolean {
    return this.getRole() === 'mentor';
  }

  isReviewer(): boolean {
    const role = this.getRole();
    return role === 'mentor' || role === 'manager' || role === 'admin';
  }

  canManageGroups(): boolean {
    return this.isAdmin() || this.isManager();
  }

  canManageMembers(): boolean {
    const role = this.getRole();
    return role !== '' && role !== 'user';
  }

  canManageMentors(): boolean {
    return this.isAdmin() || this.isManager();
  }

  canManageWarnings(): boolean {
    return this.isReviewer();
  }

  canSetupLeadership(): boolean {
    const role = this.getRole();
    return role !== '' && role !== 'user';
  }

  canReviewSkills(): boolean {
    return this.isReviewer();
  }

  getRoleSeverity(role?: string | null): string {
    const normalized = (role ?? this.getRole()).toLowerCase();
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
