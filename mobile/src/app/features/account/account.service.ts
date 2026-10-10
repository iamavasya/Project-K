import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom, timeout } from 'rxjs';
import { Api } from '../../core/api';
import { apiUrl } from '../../runtime-config';
import { healthUrl } from './account.labels';
import {
  AccountSettings,
  HealthResponse,
  MyGroupDto,
  ProblemReportPayload,
  ProblemReportReceipt,
  UpdateAccountProfileRequest,
} from './account.models';

/**
 * The «Ще» screens' calls, on the web's endpoints: account settings (account-settings.service.ts),
 * the person's гуртки (dashboard me.service.ts) and «Повідомити про проблему» (feedback.service.ts).
 */
@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly api = inject(Api);
  // `/health` is outside the API and needs no token: no interceptor, like the web's banner.
  private readonly bare = new HttpClient(inject(HttpBackend));

  settings(): Promise<AccountSettings> {
    return this.api.get('user/me');
  }

  updateProfile(request: UpdateAccountProfileRequest): Promise<AccountSettings> {
    return this.api.put('user/me', request);
  }

  changePassword(currentPassword: string, newPassword: string): Promise<boolean> {
    return this.api.post('user/me/password', { currentPassword, newPassword });
  }

  /** Провід: a new authenticator key, set up again straight after. */
  resetMfa(currentPassword: string): Promise<boolean> {
    return this.api.post('user/me/mfa/reset', { currentPassword });
  }

  /** Everyone else: the second factor off. The server refuses it to провід (`MfaRequired`). */
  disableMfa(currentPassword: string): Promise<boolean> {
    return this.api.post('user/me/mfa/disable', { currentPassword });
  }

  groups(): Promise<MyGroupDto[]> {
    return this.api.get('me/groups');
  }

  uploadScreenshot(file: Blob, name: string): Promise<{ url: string }> {
    const form = new FormData();
    form.append('file', file, name);
    return this.api.post('feedback/screenshots', form);
  }

  reportProblem(payload: ProblemReportPayload): Promise<ProblemReportReceipt> {
    return this.api.post('feedback/problems', payload);
  }

  /** The running API's version; quick, so a sleeping server never holds a screen up. */
  health(waitMs = 4000): Promise<HealthResponse> {
    return firstValueFrom(this.bare.get<HealthResponse>(healthUrl(apiUrl())).pipe(timeout({ first: waitMs })));
  }
}
