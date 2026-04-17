import { inject, Injectable } from "@angular/core";
import { environment } from "../../environments/environment";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class EntityService {
    private readonly apiUrl = environment.apiUrl;
    private readonly http = inject(HttpClient);

    checkEntityAccess(entityType: string, entityKey: string): Observable<boolean> {
        return this.http.post<boolean>(
            `${this.apiUrl}/auth/check-access`,
            { 
                entityType, 
                entityKey
            },
            { 
                headers: { 'Content-Type': 'application/json' },
                withCredentials: true 
            }
            );
    }
}
