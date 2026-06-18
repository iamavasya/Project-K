import { MemberProfileVerificationStatus } from "../../enums/member-profile-verification-status.enum";

export interface MemberLookupDto {
    memberKey: string;
    userKey?: string | null;
    userRole?: string | null;
    firstName: string;
    middleName?: string | null;
    lastName: string;
    fullNameSort?: string;
    roleSortWeight?: number;
    profilePhotoUrl?: string | null;
    latestPlastLevel?: string | null;
    latestPlastLevelDisplay?: string | null;
    phoneNumber?: string | null;
    dateOfBirth?: string | Date | null;
    profileVerificationStatus?: MemberProfileVerificationStatus;
    profileVerifiedAtUtc?: string | Date | null;
    profileVerifiedByUserKey?: string | null;
    profileVerificationNote?: string | null;
    leadershipHistories?: import('../leadership/leadershipDto').LeadershipHistoryDto[];
    warnings?: import('../../memberWarningDto').MemberWarningDto[];
}
