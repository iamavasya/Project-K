export interface BadgesMetadataDto {
  parserVersion: string;
  toolAuthor: string;
  parserComment: string;
  parsedAtUtc: string | null;
  sourceUrl: string;
  fixerEnabled: boolean;
  fixerMode: string;
  totalBadges: number;
}