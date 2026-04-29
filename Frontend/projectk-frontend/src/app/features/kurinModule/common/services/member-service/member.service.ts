import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../../../environments/environment';
import { map, Observable, throwError } from 'rxjs';
import { MemberDto } from '../../models/memberDto';
import { UpsertMemberDto } from '../../models/requests/member/upsertMemberDto';
import { MemberLookupDto } from '../../models/requests/member/memberLookupDto';
import { mapMemberForView } from '../../functions/memberViewMapper.function';

@Injectable({
  providedIn: 'root'
})
export class MemberService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/member`;

  getByKey(id: string): Observable<MemberDto> {
    return this.http.get<MemberDto>(`${this.apiUrl}/${id}`).pipe(
      map(member => mapMemberForView(member))
    );
  }

  getAll(groupKey?: string, kurinKey?: string): Observable<MemberDto[]> {
    if (!groupKey && !kurinKey) {
      return throwError(() => new Error('Either groupKey or kurinKey must be provided.'));
    }

    const requestUrl = groupKey
      ? `${this.apiUrl}/groups/${groupKey}/members`
      : `${this.apiUrl}/kurins/${kurinKey}/members`;

    return this.http.get<MemberDto[]>(requestUrl).pipe(
      map(members => members.map(member => mapMemberForView(member)))
    );
  }

  update(memberKey: string, request: UpsertMemberDto, file: Blob | null): Observable<MemberDto> {
    const formData = this.buildFormData(request, file);
    return this.http.put<MemberDto>(`${this.apiUrl}/${memberKey}`, formData).pipe(
      map(member => mapMemberForView(member))
    );
  }

  delete(memberKey: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${memberKey}`);
  }

  create(request: UpsertMemberDto, file: Blob | null): Observable<MemberDto> {
    if (!request.groupKey && !request.kurinKey) {
      return throwError(() => new Error('Either groupKey or kurinKey must be provided for create.'));
    }

    const requestUrl = request.groupKey
      ? `${this.apiUrl}`
      : `${this.apiUrl}/kurins/${request.kurinKey}/members`;

    const formData = this.buildFormData(request, file);
    return this.http.post<MemberDto>(requestUrl, formData).pipe(
      map(member => mapMemberForView(member))
    );
  }

  getKVMembers(kurinKey: string): Observable<MemberLookupDto[]> {
    return this.http.get<MemberLookupDto[]>(`${this.apiUrl}/members/kv/${kurinKey}`);
  }

  getMentorCandidates(kurinKey: string): Observable<MemberLookupDto[]> {
    return this.http.get<MemberLookupDto[]>(`${this.apiUrl}/members/mentor-candidates/${kurinKey}`);
  }

  private buildFormData(dto: UpsertMemberDto, file: Blob | null, blobFieldName = 'blob'): FormData {
      const formData = new FormData();

      Object.entries(dto).forEach(([key, value]) => {
          if (value === null || value === undefined) {
              return;
          }

        if (key === 'plastLevelHistories' && Array.isArray(value)) {
            value.forEach((historyItem, index) => {
                Object.entries(historyItem).forEach(([itemKey, itemValue]) => {
                    if (itemValue !== null && itemValue !== undefined) {
                        const formattedKey = `${key}[${index}].${itemKey}`;
                        // eslint-disable-next-line @typescript-eslint/no-explicit-any
                        formData.append(formattedKey, (itemValue as any).toString());
                    }
                });
            });
        }
          else if (value instanceof Date) {
              formData.append(key, value.toISOString());
          }
          else {
              formData.append(key, value.toString());
          }
      });

      if (file) {
          const filename = (file as File).name || 'file';
          formData.append(blobFieldName, file, filename);
      }

      return formData;
  }
}
