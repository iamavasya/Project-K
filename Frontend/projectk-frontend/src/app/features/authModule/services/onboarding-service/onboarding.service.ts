import { Injectable, inject } from '@angular/core';
import { LoginResponse } from '../../models/login-response.model';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export interface WaitlistRegistration {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: string;
  stanytsia: string;
  regionOrCountry: string;
  isKurinLeaderCandidate: boolean;
  claimedKurinNameOrNumber?: string;
}

export interface WaitlistEntry extends WaitlistRegistration {
  waitlistEntryKey: string;
  verificationStatus: string | number;
  requestedAtUtc: string;
  approvedAtUtc?: string;
  invitationSentAtUtc?: string;
  onboardingStatus?: string | number;
}

export interface InvitationValidationResponse {
  email: string;
  firstName: string;
  lastName: string;
  isValid: boolean;
}

export interface AccountActivationPayload {
  token: string | null;
  password: string;
}

export interface PasswordResetPayload {
  email: string;
  token: string;
  newPassword: string;
}

@Injectable({
  providedIn: 'root'
})
export class OnboardingService {
  private apiUrl = `${environment.apiUrl}/auth/onboarding`;
  private http = inject(HttpClient);

  submitWaitlist(registration: WaitlistRegistration): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/waitlist`, registration);
  }

  getWaitlistEntries(): Observable<WaitlistEntry[]> {
    return this.http.get<WaitlistEntry[]>(`${this.apiUrl}/waitlist`);
  }

  approveWaitlistEntry(key: string): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/waitlist/${key}/approve`, {});
  }

  rejectWaitlistEntry(key: string, note?: string): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/waitlist/${key}/reject`, JSON.stringify(note), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  resendInvitation(key: string): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/waitlist/${key}/resend-invitation`, {});
  }

  validateInvitation(token: string): Observable<InvitationValidationResponse> {
    return this.http.get<InvitationValidationResponse>(`${this.apiUrl}/invitation/${token}/validate`);
  }

  /** Answers with a signed-in session: the person lands inside the app, not on the sign-in form. */
  activateAccount(payload: AccountActivationPayload): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/activate`, payload, { withCredentials: true });
  }

  requestPasswordReset(email: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/password-reset/request`, { email });
  }

  resendInvitationByEmail(email: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/invitation/resend`, { email });
  }

  resetPassword(payload: PasswordResetPayload): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/password-reset/reset`, payload);
  }
}
