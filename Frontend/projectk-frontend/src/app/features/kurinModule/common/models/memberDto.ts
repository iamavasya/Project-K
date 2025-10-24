import { PlastLevelHistoryDto } from "./plastLevelHistoryDto";
import { LeadershipHistoryDto } from "./requests/leadership/leadershipDto";

export interface MemberDto {
    memberKey: string;
    groupKey: string;
    kurinKey: string;
    firstName: string;
    middleName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    dateOfBirth: Date | null;
    plastLevelHistories: PlastLevelHistoryDto[];
    leadershipHistories: LeadershipHistoryDto[];
    profilePhotoUrl: string | null;
}
