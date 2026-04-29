export interface LoginResponse {
    userKey: string;
    memberKey: string | null;
    email: string;
    role: string;
    kurinKey: string;
    tokens: {
        accessToken: string;
    };
}
