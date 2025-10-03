import { inject, Injectable } from "@angular/core";
import { environment } from "../../environments/environment";
import { HttpClient } from "@angular/common/http";
import { AuthService } from "./authService/auth.service";

@Injectable({
  providedIn: 'root'
})
export class EntityService {
    private readonly apiUrl = environment.apiUrl;
    private readonly http = inject(HttpClient);
    private readonly authService = inject(AuthService);

    checkEntityAccess(entityType: string, entityKey: string) {
        const activeKurinKey = this.authService.getAuthStateValue()?.kurinKey;
        console.log('checkEntityAccess', { entityType, entityKey, activeKurinKey });
        return this.http.post<boolean>(
            `${this.apiUrl}/auth/check-access`,
            { 
                entityType, 
                entityKey, 
                activeKurinKey: activeKurinKey || '' 
            },
            { 
                headers: { 'Content-Type': 'application/json' },
                withCredentials: true 
            }
            );
    }
}
