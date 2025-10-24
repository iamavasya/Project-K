export interface LeadershipDto {
    leadershipKey: string;
    type: 'kurin' | 'group' | 'kv';
    entityKey: string;
    startDate: Date;
    endDate: Date | null;
    leadershipHistories: LeadershipHistoryDto[];
}

export interface LeadershipHistoryDto {
    leadershipHistoryKey: string;
    leadershipKey: string;
    memberKey: string;
    role: string;
    startDate: Date;
    endDate: Date | null;
}