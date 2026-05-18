export type PublicAnnouncementStatus =
  | 'Draft'
  | 'PendingApproval'
  | 'Approved'
  | 'Published'
  | 'Rejected'
  | 'Deleted'
  | 'Failed';

export type PublicAnnouncementSourceType =
  | 'Manual'
  | 'Backend'
  | 'GitHubRelease'
  | 'GitHubWorkflow'
  | 'HealthMonitor';

export type PublicAnnouncementParseMode = 'PlainText' | 'Html' | 'MarkdownV2';
export type PublicAnnouncementImagePlacement = 'ImageFirst' | 'ImageLast';

export interface PublicAnnouncementDraft {
  publicAnnouncementDraftKey: string;
  status: PublicAnnouncementStatus;
  sourceType: PublicAnnouncementSourceType;
  sourceId?: string | null;
  sourceUrl?: string | null;
  environment?: string | null;
  version?: string | null;
  codename?: string | null;
  title: string;
  body: string;
  renderedText?: string | null;
  parseMode: PublicAnnouncementParseMode;
  imageBlobKey?: string | null;
  imageUrl?: string | null;
  imageAltText?: string | null;
  imagePlacement: PublicAnnouncementImagePlacement;
  templateKey?: string | null;
  templateDataJson?: string | null;
  createdByUserKey?: string | null;
  updatedByUserKey?: string | null;
  approvedByUserKey?: string | null;
  publishedByUserKey?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  approvedAtUtc?: string | null;
  publishedAtUtc?: string | null;
  telegramMessageId?: string | null;
  lastPublishError?: string | null;
}

export interface PublicAnnouncementPreview {
  renderedText: string;
  parseMode: PublicAnnouncementParseMode;
  willSendAsPhoto: boolean;
  requiresSeparateTextMessage: boolean;
  warnings: string[];
}

export interface PublicAnnouncementDraftRequest {
  title: string;
  body: string;
  sourceType?: PublicAnnouncementSourceType;
  sourceId?: string | null;
  sourceUrl?: string | null;
  environment?: string | null;
  version?: string | null;
  codename?: string | null;
  parseMode: PublicAnnouncementParseMode;
  imageBlobKey?: string | null;
  imageUrl?: string | null;
  imageAltText?: string | null;
  imagePlacement?: PublicAnnouncementImagePlacement;
  templateKey?: string | null;
  templateDataJson?: string | null;
}

export interface PublicAnnouncementImageUploadResult {
  imageBlobKey: string;
  imageUrl: string;
}

export interface PublicAnnouncementCleanupStatus {
  imageStorePath: string;
  totalLocalImages: number;
  referencedLocalImages: number;
  orphanLocalImages: number;
  eligibleForDeletion: number;
  gracePeriod: string;
  dryRun: boolean;
  checkedAtUtc: string;
}
