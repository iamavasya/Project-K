import { Injectable, inject, signal } from '@angular/core';
import { Api } from '../../core/api';
import { WaitlistEntry, awaitsDecision } from './waitlist.labels';

/**
 * The applications to join (web OnboardingService waitlist calls), for system admins only. Keeps
 * how many still wait for a word (web WaitlistAttentionService): the menu shows a dot from it, and
 * every read of the list feeds it, so a decision takes the dot away without another request.
 */
@Injectable({ providedIn: 'root' })
export class WaitlistApi {
  private readonly api = inject(Api);

  readonly pending = signal(0);

  async entries(): Promise<WaitlistEntry[]> {
    const entries = await this.api.get<WaitlistEntry[]>('auth/onboarding/waitlist');
    this.pending.set(entries.filter(awaitsDecision).length);
    return entries;
  }

  /** For the menu's dot; a failed read is not worth a word there (the page says it when opened). */
  refreshPending(): void {
    this.entries().catch(() => undefined);
  }

  /** Approves and emails the invitation link. */
  approve(key: string): Promise<string> {
    return this.api.post(`auth/onboarding/waitlist/${key}/approve`, {});
  }

  /** The note is optional; the API takes it as a bare JSON string. */
  reject(key: string, note: string | null): Promise<string> {
    return this.api.postJsonString(`auth/onboarding/waitlist/${key}/reject`, note);
  }

  /** A fresh invitation; the earlier link stops working. */
  resendInvitation(key: string): Promise<string> {
    return this.api.post(`auth/onboarding/waitlist/${key}/resend-invitation`, {});
  }
}
