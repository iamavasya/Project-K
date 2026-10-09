import { inject, Injectable } from "@angular/core";
import { environment } from "../../../../../environments/environment";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { requestFeedback } from "../../../../shared/functions/request-feedback.function";
import { ClientCacheService } from "../../../kurinModule/services/client-cache/client-cache.service";
import { ACCESS_CACHE_PREFIX, ENTITY_CACHE_TTL_MS } from "../../../kurinModule/services/client-cache/cache-policy";
import { AuthService } from "../auth-service/auth.service";

@Injectable({
  providedIn: 'root'
})
export class EntityService {
    private readonly apiUrl = environment.apiUrl;
    private readonly http = inject(HttpClient);
    private readonly cache = inject(ClientCacheService);
    private readonly authService = inject(AuthService);

    /**
     * Whether the signed-in person may act on one object. Answered once a minute per object: the
     * guard and the pages each asked on every navigation. The kurin is in the key because the same
     * object answers differently from another kurin, and the person because a cached answer must
     * not outlive a sign-out in a shared browser. A refused answer is not kept.
     */
    checkEntityAccess(entityType: string, entityKey: string, action?: string): Observable<boolean> {
        const state = this.authService.getAuthStateValue();
        const key = `${ACCESS_CACHE_PREFIX}${state?.kurinKey ?? '-'}:${state?.userKey ?? '-'}:${entityType}:${entityKey}:${action ?? ''}`;

        return this.cache.get(key, ENTITY_CACHE_TTL_MS, () => this.http.post<boolean>(
            `${this.apiUrl}/auth/check-access`,
            {
                entityType,
                entityKey,
                action
            },
            {
                headers: { 'Content-Type': 'application/json' },
                withCredentials: true,
                context: requestFeedback('silent')
            }
        ));
    }
}
