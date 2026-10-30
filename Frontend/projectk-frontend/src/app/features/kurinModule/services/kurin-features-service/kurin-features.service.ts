import { Injectable, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { AuthService } from '../../../authModule/services/auth-service/auth.service';
import { KurinService } from '../kurin-service/kurin.service';
import { hasGroups, isSeniorBranch } from '../../models/enums/kurin-branch.enum';

/**
 * Чим користується курінь, у якому людина діє зараз: є в ньому гуртки чи ні, ведеться точкування.
 * Для компонентів, яким курінь не передають, а слова й блоки залежать від нього. Курінь не
 * прочитався — вважаємо, що все є: краще зайвий блок, ніж зниклий.
 */
@Injectable({ providedIn: 'root' })
export class KurinFeaturesService {
  private readonly auth = inject(AuthService);
  private readonly kurins = inject(KurinService);

  private readonly kurin = toSignal(
    this.auth.getAuthState().pipe(
      map(state => state?.kurinKey ?? null),
      distinctUntilChanged(),
      switchMap(key => key ? this.kurins.getByKey(key).pipe(catchError(() => of(null))) : of(null))
    ),
    { initialValue: null }
  );

  /** Гуртки є в усіх куренях, крім старших. */
  readonly hasGroups = computed(() => hasGroups(this.kurin()?.branch));

  readonly isSenior = computed(() => isSeniorBranch(this.kurin()?.branch));
}
