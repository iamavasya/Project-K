import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { catchError, firstValueFrom, of, Subject, take, timeout } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class HealthBannerService {
  private readonly httpBackend = inject(HttpBackend);
  private readonly http = new HttpClient(this.httpBackend);
  private readonly bannerVisible = signal(false);
  private readonly isPolling = signal(false);
  private readonly hasConfirmedHealthy = signal(false);
  private readonly hasStartedSessionCheck = signal(false);
  private pollingTimer: ReturnType<typeof setTimeout> | null = null;
  private readonly backoffDelaysMs = [3000, 5000, 10000];
  private readonly healthUrl = this.buildHealthUrl(environment.apiUrl);
  private readonly healthReady$ = new Subject<void>();

  readonly isBannerVisible = this.bannerVisible.asReadonly();

  shouldGateRequests(): boolean {
    return this.isEnabled() && !this.hasConfirmedHealthy();
  }

  waitForHealthy() {
    if (!this.isEnabled()) {
      return of(void 0);
    }

    if (!this.isPolling()) {
      this.isPolling.set(true);
      void this.runCheckSequence();
    }

    return this.healthReady$.pipe(take(1));
  }

  startSessionCheck(): void {
    if (!this.isEnabled() || this.hasStartedSessionCheck()) {
      return;
    }

    this.hasStartedSessionCheck.set(true);
    this.isPolling.set(true);
    void this.runCheckSequence();
  }

  private async runCheckSequence(): Promise<void> {
    const isHealthy = await this.checkHealthOnce();
    if (isHealthy) {
      this.setHealthy();
      return;
    }

    this.bannerVisible.set(true);
    this.scheduleNextPing(0);
  }

  private async runPollingAttempt(attempt: number): Promise<void> {
    const isHealthy = await this.checkHealthOnce();
    if (isHealthy) {
      this.setHealthy();
      return;
    }

    this.bannerVisible.set(true);
    const nextAttempt = Math.min(attempt + 1, this.backoffDelaysMs.length - 1);
    this.scheduleNextPing(nextAttempt);
  }

  private scheduleNextPing(attempt: number): void {
    const fallbackDelay = this.backoffDelaysMs.at(-1) ?? 0;
    const delay = this.backoffDelaysMs[attempt] ?? fallbackDelay;
    this.clearTimer();
    this.pollingTimer = setTimeout(() => {
      void this.runPollingAttempt(attempt);
    }, delay);
  }

  private async checkHealthOnce(): Promise<boolean> {
    const response = await firstValueFrom(
      this.http.get(this.healthUrl, { responseType: 'text' }).pipe(
        timeout({ first: 2000 }),
        catchError(() => of(null))
      )
    );

    return response !== null;
  }

  private stopPolling(): void {
    this.clearTimer();
    this.isPolling.set(false);
    this.bannerVisible.set(false);
  }

  private setHealthy(): void {
    this.hasConfirmedHealthy.set(true);
    this.healthReady$.next();
    this.stopPolling();
  }

  private clearTimer(): void {
    if (this.pollingTimer) {
      clearTimeout(this.pollingTimer);
      this.pollingTimer = null;
    }
  }

  private isEnabled(): boolean {
    return environment.isF1TierBackend === true;
  }

  private buildHealthUrl(apiUrl: string): string {
    const trimmed = apiUrl.endsWith('/') ? apiUrl.slice(0, -1) : apiUrl;

    if (trimmed.endsWith('/api')) {
      return `${trimmed.slice(0, -4)}/health`;
    }

    return `${trimmed}/health`;
  }
}
