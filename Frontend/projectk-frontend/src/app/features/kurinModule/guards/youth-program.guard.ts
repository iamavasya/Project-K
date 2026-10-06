import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { KurinService } from '../services/kurin-service/kurin.service';
import { hasYouthProgram } from '../models/enums/kurin-branch.enum';

/**
 * Сторінки юнацького вишколу куреня — лише в курені УПЮ. У курені УСП чи УПС такої сторінки немає,
 * тож пряме посилання веде на чесне «тут такого немає», а не на порожній екран.
 *
 * Курінь не прочитався — пропускаємо: сторінка й сама спитає сервер, а він і є справжня перевірка.
 */
export const youthProgramGuard = (kurinKeyParam = 'kurinKey'): CanActivateFn => {
  return route => {
    const kurinService = inject(KurinService);
    const router = inject(Router);
    const kurinKey = route.paramMap.get(kurinKeyParam);
    if (!kurinKey) {
      return true;
    }

    return kurinService.getByKey(kurinKey).pipe(
      map(kurin => hasYouthProgram(kurin.branch) ? true : router.createUrlTree(['/no-youth-program'])),
      catchError(() => of(true))
    );
  };
};
