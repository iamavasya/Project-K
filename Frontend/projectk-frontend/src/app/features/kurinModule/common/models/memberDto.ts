import { PlastLevelHistoryDto } from "./plastLevelHistoryDto";
import { LeadershipHistoryDto } from "./requests/leadership/leadershipDto";
import { MemberWarningDto } from "./memberWarningDto";
import { MemberAwardDto } from "./memberAwardDto";

export interface MemberDto {
    memberKey: string;
    groupKey: string;
    kurinKey: string;
    userKey?: string | null;
    firstName: string;
    middleName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    dateOfBirth: Date | null;
    address?: string | null;
    school?: string | null;
    latestPlastLevel?: string | null;
    latestPlastLevelDisplay?: string | null;
    plastLevelHistories: PlastLevelHistoryDto[];
    leadershipHistories: LeadershipHistoryDto[];
    warnings?: MemberWarningDto[];
    awards?: MemberAwardDto[];
    profilePhotoUrl: string | null;
}
