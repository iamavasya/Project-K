import { BadgeProgressStatus } from "./enums/badge-progress-status.enum";
import { MemberAwardLevel } from "./enums/member-award-level.enum";

export interface MemberAwardDto {
    memberAwardKey: string;
    memberKey: string;
    kurinKey: string;
    level: MemberAwardLevel;
    dateAcquired: string;
    note?: string;
    status: BadgeProgressStatus;
    imageUrl?: string;
    submittedAtUtc?: string;
    submittedByUserKey?: string;
    reviewedAtUtc?: string;
    reviewedByUserKey?: string;
}
