import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface WaitlistRegistration {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: string;
  isKurinLeaderCandidate: boolean;
  claimedKurinNameOrNumber?: string;
}

export interface WaitlistEntry extends WaitlistRegistration {
  waitlistEntryKey: string;
  verificationStatus: number;
  requestedAtUtc: string;
  approvedAtUtc?: string;
  invitationSentAtUtc?: string;
  onboardingStatus?: number;
}

export interface InvitationValidationResponse {
  email: string;
  firstName: string;
  lastName: string;
  isValid: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class OnboardingService {
  private apiUrl = `${environment.apiUrl}/auth/onboarding`;

  constructor(private http: HttpClient) {}

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

  activateAccount(payload: any): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/activate`, payload);
  }

  requestPasswordReset(email: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/password-reset/request`, { email });
  }

  resetPassword(payload: any): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/password-reset/reset`, payload);
  }
}
