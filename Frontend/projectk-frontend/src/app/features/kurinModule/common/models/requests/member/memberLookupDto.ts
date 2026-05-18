export interface MemberLookupDto {
    memberKey: string;
    userKey?: string | null;
    userRole?: string | null;
    firstName: string;
    middleName?: string | null;
    lastName: string;
    profilePhotoUrl?: string | null;
    latestPlastLevel?: string | null;
    latestPlastLevelDisplay?: string | null;
    phoneNumber?: string | null;
    dateOfBirth?: string | Date | null;
    leadershipHistories?: import('../leadership/leadershipDto').LeadershipHistoryDto[];
    warnings?: import('../../memberWarningDto').MemberWarningDto[];
}
