import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { catchError, map, Observable, of } from 'rxjs';
import { GroupDto } from '../../models/groupDto';
import { CreateGroupDto } from '../../models/requests/createGroupDto';
import { UpdateGroupDto } from '../../models/requests/updateGroupDto';

@Injectable({
  providedIn: 'root'
})
export class GroupService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/group`;

  getByKey(groupKey: string): Observable<GroupDto> {
    return this.http.get<GroupDto>(`${this.apiUrl}/${groupKey}`);
  }

  getAllByKurinKey(kurinKey: string): Observable<GroupDto[]> {
    return this.http.get<GroupDto[]>(`${this.apiUrl}/groups`, { params: { kurinKey } });
  }

  create(request: CreateGroupDto): Observable<GroupDto> {
    return this.http.post<GroupDto>(`${this.apiUrl}`, request);
  }

  update(groupKey: string, request: UpdateGroupDto): Observable<GroupDto> {
    return this.http.put<GroupDto>(`${this.apiUrl}/${groupKey}`, request);
  }

  delete(groupKey: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${groupKey}`);
  }
    
  exists(groupKey: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.apiUrl}/exists/${groupKey}`);
  }
}
