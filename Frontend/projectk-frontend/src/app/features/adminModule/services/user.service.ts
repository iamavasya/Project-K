import { inject, Injectable } from "@angular/core";
import { environment } from "../../../../environments/environment";
import { HttpClient } from "@angular/common/http";
import { UserDto } from "../models/userDto";

@Injectable({
    providedIn: 'root'
})
export class UserService {
    private readonly apiUrl = environment.apiUrl;
    private readonly http = inject(HttpClient);

    getAllUsers() {
        return this.http.get<UserDto[]>(
            `${this.apiUrl}/user/users`,
            { withCredentials: true }
        );
    }

    changeUserRole(userId: string, newRole: number) {
        return this.http.post<boolean>(
            `${this.apiUrl}/user/${userId}/role`,
            newRole,
            { withCredentials: true, headers: { 'Content-Type': 'application/json' } }
        );
    }

    deleteUser(userId: string) {
        return this.http.delete<boolean>(
            `${this.apiUrl}/user/${userId}`,
            { withCredentials: true }
        );
    }
}