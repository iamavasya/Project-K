import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { MemberDto } from '../../models/memberDto';
import { UpsertMemberDto } from '../../models/requests/member/upsertMemberDto';

@Injectable({
  providedIn: 'root'
})
export class MemberService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/member`;
  private readonly guidNull = '00000000-0000-0000-0000-000000000000';

  getByKey(id: string): Observable<MemberDto> {
    return this.http.get<MemberDto>(`${this.apiUrl}/${id}`);
  }

  getAll(groupKey?: string, kurinKey?: string): Observable<MemberDto[]> {
    return this.http.get<MemberDto[]>(`${this.apiUrl}/members`, { params: { groupKey: groupKey ?? this.guidNull, kurinKey: kurinKey ?? this.guidNull } });
  }

  update(memberKey: string, request: UpsertMemberDto): Observable<MemberDto> {
    return this.http.put<MemberDto>(`${this.apiUrl}/${memberKey}`, request);
  }

  delete(memberKey: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${memberKey}`);
  }

  create(request: UpsertMemberDto): Observable<MemberDto> {
    return this.http.post<MemberDto>(`${this.apiUrl}`, request);
  }
}
