/**
 * The system-level roles, by name. The admin screen used to post the enum's ordinals
 * (Admin = 0, Member = 1), so reordering UserRole on the backend would have silently
 * assigned the wrong one.
 */
export type SystemUserRole = 'Admin' | 'Member';

export interface UserDto {
    userId: string;
    email: string;
    role: string;
    kurinKey: string | null;
    kurinNumber: number | null;
    firstName: string;
    lastName: string;
}