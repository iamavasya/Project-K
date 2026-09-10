import { PlastLevelHistoryDto } from "./plastLevelHistoryDto";
import { LeadershipHistoryDto } from "./requests/leadership/leadershipDto";
import { MemberWarningDto } from "./memberWarningDto";
import { MemberAwardDto } from "./memberAwardDto";
import { MemberProfileVerificationStatus } from "./enums/member-profile-verification-status.enum";

export interface MemberDto {
    memberKey: string;
    /** Код, який людина віддає проводу іншого куреня, щоб та прийняла її. Свій — бачить лише вона. */
    publicId?: string;
    groupKey: string;
    /** Назва гуртка. Її знає лише читання списку — картка бере гурток із членства. */
    groupName?: string | null;
    kurinKey: string;
    userKey?: string | null;
    userRole?: string | null;
    /**
     * Чи людина в кадрі виховників цього куреня. Заповнює лише читання списку — картка про одну
     * людину не знає, про яке саме членство йдеться.
     */
    isStaff?: boolean;
    /** Гуртки, за якими людина закріплена як виховник. Порожньо в юнака. */
    mentoredGroupNames?: string[];
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
    profileVerificationStatus?: MemberProfileVerificationStatus;
    profileVerifiedAtUtc?: string | Date | null;
    profileVerifiedByUserKey?: string | null;
    profileVerificationNote?: string | null;
}
