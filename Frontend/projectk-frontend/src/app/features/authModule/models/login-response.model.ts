export interface LoginResponse {
    userKey: string;
    memberKey: string | null;
    email: string;
    role: string;
    kurinKey: string;
    requiresMfa: boolean;
    tokens: {
        accessToken: string;
    } | null;
}
