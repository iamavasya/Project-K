export interface MemberAvailability {
  memberKey: string;
  fullName: string;
  roleWeight: number;
  busyRanges: Date[][]; 
}