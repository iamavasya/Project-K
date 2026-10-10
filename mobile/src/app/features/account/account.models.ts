import { MyKurinRefDto } from '../../me/me.models';

/**
 * The account's own settings, `GET/PUT user/me`. Kept in step with the web's
 * account-settings.service.ts and the API's AccountSettingsDto.
 */
export interface AccountSettings {
  userKey: string;
  memberKey: string | null;
  email: string;
  phoneNumber: string | null;
  firstName: string;
  lastName: string;
  role: string;
  twoFactorEnabled: boolean;
  /** The new address while it waits for the link in the confirmation letter. */
  pendingEmail: string | null;
}

export interface UpdateAccountProfileRequest {
  email: string;
  phoneNumber: string | null;
  /** Only when the email changes. */
  currentPassword: string | null;
}

/** A гурток the person stands in or leads, `GET me/groups` (web dashboardModule/models/me.dto.ts). */
export interface MyGroupDto {
  groupKey: string;
  kurin: MyKurinRefDto;
  name: string;
  silhouetteUrl: string | null;
  isOwn: boolean;
  isLed: boolean;
}

/**
 * «Повідомити про проблему», `POST feedback/problems`: the web's feedback.service.ts shapes and the
 * API's FeedbackController.ReportProblemRequest.
 */
export interface ProblemReportPayload {
  title: string | null;
  description: string;
  steps: string | null;
  expected: string | null;
  route: string;
  appVersion: string;
  screenshotUrls: string[];
}

/** Where the report went; both null when the server only logged it. */
export interface ProblemReportReceipt {
  issueUrl: string | null;
  issueNumber: number | null;
}

/** What `/health` says about the running API (web about-page.ts). */
export interface HealthResponse {
  version?: string;
  codeName?: string | null;
}
