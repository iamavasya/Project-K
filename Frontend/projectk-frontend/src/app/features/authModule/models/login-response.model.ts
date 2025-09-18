export interface LoginResponse {
    userKey: string;
    email: string;
    role: string;
    tokens: {
        accessToken: string;
        refreshToken: {
            token: string;
            expires: string;
            created: string;
            createdByIp?: string | null;
            revoked?: string | null;
            revokedByIp?: string | null;
            replacedByToken?: string | null;
            isExpired?: boolean;
            isActive?: boolean;
        }
    }
}

export interface UserSession {
    accessToken: string;
    refreshToken: string;
    expires: Date;
}