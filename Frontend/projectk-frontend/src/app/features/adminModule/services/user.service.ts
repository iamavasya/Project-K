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
}