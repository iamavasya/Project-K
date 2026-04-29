export interface UserDto {
    userId: string;
    email: string;
    role: string;
    kurinKey: string | null;
    kurinNumber: number | null;
    firstName: string;
    lastName: string;
}