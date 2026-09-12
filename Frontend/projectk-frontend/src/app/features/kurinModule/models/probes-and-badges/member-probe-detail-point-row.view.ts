export interface MemberProbeDetailPointRowView {
  sectionId: string;
  sectionCode: string;
  sectionTitle: string;
  pointId: string;
  pointTitle: string;
  isSigned: boolean;
  signedByUserKey: string | null;
  signedByName: string | null;
  signedByRole: string | null;
  signedAtUtc: string | null;
}
