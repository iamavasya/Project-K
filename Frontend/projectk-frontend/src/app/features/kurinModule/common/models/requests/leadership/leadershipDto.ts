import { MemberLookupDto } from "../member/memberLookupDto";

export interface LeadershipDto {
    leadershipKey: string | null;
    type?: 'kurin' | 'group' | 'kv';
    entityKey?: string;
    startDate: string;
    endDate: string | null;
    leadershipHistories: LeadershipHistoryDto[];
}

export interface LeadershipHistoryDto {
    leadershipHistoryKey: string;
    leadershipKey: string;
    role: string;
    leadershipType?: 'Kurin' | 'Group' | 'KV' | 'kurin' | 'group' | 'kv' | null;
    groupName?: string | null;
    startDate: string;
    endDate: string | null;
    member: MemberLookupDto;
}
