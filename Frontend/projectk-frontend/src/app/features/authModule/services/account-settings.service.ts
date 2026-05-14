import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface AccountSettings {
  userKey: string;
  memberKey: string | null;
  email: string;
  phoneNumber: string | null;
  firstName: string;
  lastName: string;
  role: string;
  twoFactorEnabled: boolean;
  pendingEmail: string | null;
}

export interface UpdateAccountProfileRequest {
  email: string;
  phoneNumber: string | null;
  currentPassword: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ConfirmAccountEmailChangeRequest {
  email: string;
  token: string;
}

export interface ResetMfaRequest {
  currentPassword: string;
}

export interface DisableMfaRequest {
  currentPassword: string;
}

@Injectable({
  providedIn: 'root'
})
export class AccountSettingsService {
  private readonly apiUrl = environment.apiUrl;
  private readonly http = inject(HttpClient);

  getSettings(): Observable<AccountSettings> {
    return this.http.get<AccountSettings>(`${this.apiUrl}/user/me`, { withCredentials: true });
  }

  updateProfile(request: UpdateAccountProfileRequest): Observable<AccountSettings> {
    return this.http.put<AccountSettings>(`${this.apiUrl}/user/me`, request, { withCredentials: true });
  }

  confirmEmailChange(request: ConfirmAccountEmailChangeRequest): Observable<AccountSettings> {
    return this.http.post<AccountSettings>(`${this.apiUrl}/user/me/email/confirm`, request, { withCredentials: true });
  }

  changePassword(request: ChangePasswordRequest): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/user/me/password`, request, { withCredentials: true });
  }

  resetMfa(request: ResetMfaRequest): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/user/me/mfa/reset`, request, { withCredentials: true });
  }

  disableMfa(request: DisableMfaRequest): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/user/me/mfa/disable`, request, { withCredentials: true });
  }
}
