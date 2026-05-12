import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../../../environments/environment';
import { MemberWarningDto } from '../../models/memberWarningDto';
import { AssignMemberWarningRequestDto } from '../../models/requests/member/assignMemberWarningRequestDto';

@Injectable({
  providedIn: 'root'
})
export class MemberWarningService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/member`;

  getWarnings(memberKey: string): Observable<MemberWarningDto[]> {
    return this.http.get<MemberWarningDto[]>(`${this.apiUrl}/${memberKey}/warnings`);
  }

  assignWarning(memberKey: string, request: AssignMemberWarningRequestDto): Observable<MemberWarningDto> {
    return this.http.post<MemberWarningDto>(`${this.apiUrl}/${memberKey}/warnings`, request);
  }

  cancelWarning(memberKey: string, warningKey: string): Observable<MemberWarningDto> {
    return this.http.delete<MemberWarningDto>(`${this.apiUrl}/${memberKey}/warnings/${warningKey}`);
  }
}
