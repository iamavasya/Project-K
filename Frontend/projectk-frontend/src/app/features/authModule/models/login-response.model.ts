export interface LoginResponse {
    userKey: string;
    memberKey: string | null;
    email: string;
    isAdmin: boolean;
    permissions: string[];
    roles: string[];
    kurinKey: string;
    requiresMfa: boolean;
    /**
     * Proof that the password step passed. Comes only with `requiresMfa`, and the second step has
     * to send it back — a code on its own is refused.
     */
    mfaToken?: string | null;
    tokens: {
        accessToken: string;
    } | null;
}
