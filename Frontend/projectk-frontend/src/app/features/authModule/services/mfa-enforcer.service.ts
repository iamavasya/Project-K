import { inject, Injectable } from '@angular/core';
import { AuthService } from './authService/auth.service';
import { PermissionService } from './permission.service';
import { catchError, of, switchMap } from 'rxjs';
import { MfaSetupDialogComponent } from '../components/mfa-setup-dialog/mfa-setup-dialog.component';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MfaEnforcerService {
  private readonly authService = inject(AuthService);
  private readonly permissionService = inject(PermissionService);

  checkAndEnforce(dialog: MfaSetupDialogComponent): void {
    if (!environment.production) {
      return;
    }
    this.authService.getAuthState().pipe(
      switchMap(state => {
        if (!state?.accessToken) {
          return of(null);
        }

        const isPrivileged = this.permissionService.isAdmin(state.role) || this.permissionService.isManager(state.role);
        return isPrivileged
          ? this.authService.getMfaStatus().pipe(catchError(() => of(null)))
          : of(null);
      })
    ).subscribe(status => {
      if (status && !status.isMfaEnabled && !dialog.visible) {
        dialog.show(true);
      }
    });
  }
}
