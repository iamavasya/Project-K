import { MemberLookupDto } from "../member/memberLookupDto";

export interface LeadershipDto {
    leadershipKey: string | null;
    type?: 'kurin' | 'group' | 'kv';
    entityKey?: string;
    startDate: Date;
    endDate: Date | null;
    leadershipHistories: LeadershipHistoryDto[];
}

export interface LeadershipHistoryDto {
    leadershipHistoryKey: string;
    leadershipKey: string;
    role: string;
    startDate: Date;
    endDate: Date | null;
    member: MemberLookupDto;
}