import { inject, Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, CanActivate, Router } from "@angular/router";
import { HttpErrorResponse } from "@angular/common/http";
import { catchError, map, Observable, of } from "rxjs";
import { EntityService } from "../services/entity.service";

@Injectable({
  providedIn: 'root'
})
export class EntityGuard implements CanActivate {
    private readonly router = inject(Router);
    private readonly entityService = inject(EntityService);

    canActivate(route: ActivatedRouteSnapshot): Observable<boolean> {
        const entityType = this.resolveEntityType(route);
        const entityKey = this.resolveEntityKey(route, entityType);

        // EntityGuard is a UX pre-check only; backend remains the source of truth.
        if (!entityType || !entityKey) {
            return of(true);
        }

        return this.entityService.checkEntityAccess(entityType, entityKey).pipe(
            map(access => {
                if (access) {
                    return true;
                } else {
                    this.router.navigate(['/forbidden']);
                    return false;
                }
            }),
            catchError((error: unknown) => {
                if (error instanceof HttpErrorResponse && error.status === 403) {
                    this.router.navigate(['/forbidden']);
                    return of(false);
                }

                // Do not hard-block navigation on transport/transient errors.
                return of(true);
            })
        );
    }

    private resolveEntityType(route: ActivatedRouteSnapshot): string | null {
        const routeType = route.data['entityType'];
        if (typeof routeType === 'string' && routeType.length > 0) {
            return routeType;
        }

        const entityTypeParam = route.data['entityTypeParam'];
        if (typeof entityTypeParam === 'string' && entityTypeParam.length > 0) {
            return route.paramMap.get(entityTypeParam);
        }

        return null;
    }

    private resolveEntityKey(route: ActivatedRouteSnapshot, entityType: string | null): string | null {
        const entityKeyParam = route.data['entityKeyParam'];
        if (typeof entityKeyParam === 'string' && entityKeyParam.length > 0) {
            return route.paramMap.get(entityKeyParam);
        }

        if (!entityType) {
            return null;
        }

        return route.paramMap.get(`${entityType}Key`);
    }
}
