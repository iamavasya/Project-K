export interface UserDto {
    userId: string;
    email: string;
    role: string;
    kurinKey: string | null;
    firstName: string;
    lastName: string;
}