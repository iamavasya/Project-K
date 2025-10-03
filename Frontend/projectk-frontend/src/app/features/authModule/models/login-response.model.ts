export interface LoginResponse {
    userKey: string;
    email: string;
    role: string;
    kurinKey: string;
    tokens: {
        accessToken: string;
    };
}
