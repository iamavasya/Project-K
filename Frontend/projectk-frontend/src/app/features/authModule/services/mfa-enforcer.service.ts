import { inject, Injectable } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { AuthService } from './authService/auth.service';
import { PermissionService } from './permission.service';
import { catchError, filter, merge, of, Subscription } from 'rxjs';
import { MfaSetupDialogComponent } from '../components/mfa-setup-dialog/mfa-setup-dialog.component';
import { environment } from '../../../../environments/environment';
import { getMfaSetupRequiredKey, getMfaStatusCheckedKey } from './mfa-session-state';

@Injectable({
  providedIn: 'root'
})
export class MfaEnforcerService {
  private readonly authService = inject(AuthService);
  private readonly permissionService = inject(PermissionService);
  private readonly router = inject(Router);
  private dialog: MfaSetupDialogComponent | null = null;
  private subscription: Subscription | null = null;
  private checkingStatus = false;

  checkAndEnforce(dialog: MfaSetupDialogComponent): void {
    if (!environment.production) {
      return;
    }

    this.dialog = dialog;

    if (this.subscription) {
      return;
    }

    const routeChanges$ = this.router.events.pipe(filter(event => event instanceof NavigationEnd));

    this.subscription = merge(this.authService.getAuthState(), routeChanges$)
      .subscribe(() => this.enforceCurrentSessionState());

    this.enforceCurrentSessionState();
  }

  markMfaEnabledForCurrentSession(): void {
    const userKey = this.authService.getAuthStateValue()?.userKey;
    if (!userKey) {
      return;
    }

    sessionStorage.setItem(this.getCheckedKey(userKey), 'true');
    sessionStorage.removeItem(this.getRequiredKey(userKey));
  }

  private enforceCurrentSessionState(): void {
    const state = this.authService.getAuthStateValue();
    if (!state?.userKey || !state.accessToken || !this.dialog) {
      return;
    }

    const isPrivileged = this.permissionService.isAdmin(state.role) || this.permissionService.isManager(state.role);
    if (!isPrivileged) {
      return;
    }

    if (sessionStorage.getItem(this.getRequiredKey(state.userKey)) === 'true') {
      this.showRequiredDialog();
      return;
    }

    if (sessionStorage.getItem(this.getCheckedKey(state.userKey)) === 'true' || this.checkingStatus) {
      return;
    }

    this.checkingStatus = true;
    this.authService.getMfaStatus().pipe(
      catchError(() => of(null))
    ).subscribe(status => {
      this.checkingStatus = false;

      if (!status) {
        return;
      }

      if (status.isMfaEnabled || !status.isMfaRequired) {
        this.markMfaEnabledForCurrentSession();
        return;
      }

      sessionStorage.setItem(this.getRequiredKey(state.userKey), 'true');
      this.showRequiredDialog();
    });
  }

  private showRequiredDialog(): void {
    if (this.dialog && !this.dialog.visible) {
      this.dialog.show(true);
    }
  }

  private getCheckedKey(userKey: string): string {
    return getMfaStatusCheckedKey(userKey);
  }

  private getRequiredKey(userKey: string): string {
    return getMfaSetupRequiredKey(userKey);
  }
}
