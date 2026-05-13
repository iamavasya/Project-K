import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../../../environments/environment';
import { MemberAwardDto } from '../../models/memberAwardDto';
import { MemberAwardLevel } from '../../models/enums/member-award-level.enum';
import { ReviewBadgeProgressRequestDto } from '../../models/probes-and-badges/requests/reviewBadgeProgressRequestDto';

export interface UpsertMemberAwardRequest {
    memberAwardKey?: string;
    level: MemberAwardLevel;
    dateAcquired: string;
    note?: string;
}

@Injectable({
    providedIn: 'root'
})
export class MemberAwardService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/member`;

    upsertAward(memberKey: string, request: UpsertMemberAwardRequest): Observable<MemberAwardDto> {
        return this.http.post<MemberAwardDto>(`${this.apiUrl}/${memberKey}/awards`, request);
    }

    deleteAward(memberKey: string, awardKey: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${memberKey}/awards/${awardKey}`);
    }

    reviewAward(memberKey: string, awardKey: string, request: ReviewBadgeProgressRequestDto): Observable<MemberAwardDto> {
        return this.http.post<MemberAwardDto>(`${this.apiUrl}/${memberKey}/awards/${awardKey}/review`, request);
    }
}
