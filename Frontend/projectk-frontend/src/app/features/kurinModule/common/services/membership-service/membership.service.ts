import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../../../../environments/environment';
import { ClientCacheService } from '../client-cache/client-cache.service';
import { MEMBER_CACHE_PREFIX, GROUP_CACHE_PREFIX, KURIN_CACHE_PREFIX } from '../client-cache/cache-policy';
import { MembershipKind } from '../../models/enums/membership-kind.enum';

/** Картка людини, яку провід бачить перед тим, як прийняти її за кодом. Куренів вона не називає. */
export interface MemberCardDto {
  memberKey: string;
  publicId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  profilePhotoBlobName?: string | null;
  currentMembershipCount: number;
}

export interface MembershipCandidateDto {
  member: MemberCardDto;
  alreadyInThisKurin: boolean;
}

/**
 * Належність до куреня — окремо від профілю людини. Профіль редагується один раз і скрізь, а
 * членство відкривається й закривається в кожному курені окремо, і це різні дії з різними правами.
 */
@Injectable({ providedIn: 'root' })
export class MembershipService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(ClientCacheService);
  private readonly apiUrl = `${environment.apiUrl}/kurin`;

  findCandidate(kurinKey: string, publicId: string): Observable<MembershipCandidateDto> {
    return this.http.get<MembershipCandidateDto>(
      `${this.apiUrl}/${kurinKey}/memberships/candidate`,
      { params: { publicId } }
    );
  }

  joinByPublicId(kurinKey: string, publicId: string, kind = MembershipKind.Youth): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/${kurinKey}/memberships`, { publicId, kind }).pipe(
      tap(() => this.forgetPlacement())
    );
  }

  leave(kurinKey: string, memberKey: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${kurinKey}/memberships/${memberKey}`).pipe(
      tap(() => this.forgetPlacement())
    );
  }

  moveToGroup(kurinKey: string, memberKey: string, groupKey: string | null): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${kurinKey}/memberships/${memberKey}/group`,
      { groupKey }
    ).pipe(tap(() => this.forgetPlacement()));
  }

  /** Членство змінює склад списків куреня й гуртка так само, як самої людини. */
  private forgetPlacement(): void {
    this.cache.invalidateByPrefix(MEMBER_CACHE_PREFIX);
    this.cache.invalidateByPrefix(GROUP_CACHE_PREFIX);
    this.cache.invalidateByPrefix(KURIN_CACHE_PREFIX);
  }
}
