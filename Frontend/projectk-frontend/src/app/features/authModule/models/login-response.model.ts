export interface LoginResponse {
    userKey: string;
    memberKey: string | null;
    email: string;
    isAdmin: boolean;
    permissions: string[];
    roles: string[];
    kurinKey: string;
    requiresMfa: boolean;
    tokens: {
        accessToken: string;
    } | null;
}
