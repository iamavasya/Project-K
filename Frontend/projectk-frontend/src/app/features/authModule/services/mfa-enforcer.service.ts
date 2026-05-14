import { inject, Injectable } from '@angular/core';
import { AuthService } from './authService/auth.service';
import { PermissionService } from './permission.service';
import { interval, merge, of, startWith, switchMap } from 'rxjs';
import { MfaSetupDialogComponent } from '../components/mfa-setup-dialog/mfa-setup-dialog.component';

@Injectable({
  providedIn: 'root'
})
export class MfaEnforcerService {
  private readonly authService = inject(AuthService);
  private readonly permissionService = inject(PermissionService);

  checkAndEnforce(dialog: MfaSetupDialogComponent): void {
    merge(this.authService.getAuthState(), interval(30000).pipe(startWith(0), switchMap(() => of(this.authService.getAuthStateValue())))).pipe(
      switchMap(state => {
        if (!state) {
          return of(null);
        }

        const isPrivileged = this.permissionService.isAdmin(state.role) || this.permissionService.isManager(state.role);
        return isPrivileged ? this.authService.getMfaStatus() : of(null);
      })
    ).subscribe(status => {
      if (status && !status.isMfaEnabled && !dialog.visible) {
        dialog.show(true);
      }
    });
  }
}
