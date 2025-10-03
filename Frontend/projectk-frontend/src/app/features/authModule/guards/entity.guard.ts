import { inject, Injectable } from "@angular/core";
import { AuthService } from "../services/authService/auth.service";
import { ActivatedRouteSnapshot, CanActivate, Router } from "@angular/router";
import { catchError, map, Observable, of } from "rxjs";
import { EntityService } from "../services/entity.service";

@Injectable({
  providedIn: 'root'
})

// TODO: Переробити під бекенд через мідлверки
export class EntityGuard implements CanActivate {
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);
    private readonly entityService = inject(EntityService);

    canActivate(route: ActivatedRouteSnapshot): Observable<boolean> {
        const activeKurinKey = this.authService.getAuthStateValue()?.kurinKey;
        console.log('kurinkey', activeKurinKey);
        const entityType = route.data['entityType'];
        console.log('entityType', entityType);
        const entityKey = route.paramMap.get(`${entityType}Key`);
        console.log('entityKey', entityKey);

        if (!activeKurinKey || !entityKey) {
            this.router.navigate(['/']);
            return of(false);
        }

        return this.entityService.checkEntityAccess(entityType, entityKey).pipe(
            map(access => {
                if (access) {
                    return true;
                } else {
                    this.router.navigate(['/kurin']);
                    return false;
                }
            }),
            catchError(() => {
                this.router.navigate(['']);
                return of(false);
            })
        );
    }
}
