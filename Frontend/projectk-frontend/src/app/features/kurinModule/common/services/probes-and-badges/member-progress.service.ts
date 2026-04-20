import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../../../environments/environment';
import { BadgeProgressDto } from '../../models/probes-and-badges/badgeProgressDto';
import { ProbeProgressDto } from '../../models/probes-and-badges/probeProgressDto';
import { ReviewBadgeProgressRequestDto } from '../../models/probes-and-badges/requests/reviewBadgeProgressRequestDto';
import { SubmitBadgeProgressRequestDto } from '../../models/probes-and-badges/requests/submitBadgeProgressRequestDto';
import { UpdateProbePointSignatureRequestDto } from '../../models/probes-and-badges/requests/updateProbePointSignatureRequestDto';
import { UpdateProbeProgressStatusRequestDto } from '../../models/probes-and-badges/requests/updateProbeProgressStatusRequestDto';

@Injectable({
  providedIn: 'root'
})
export class MemberProgressService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/member`;

  getBadgeProgresses(memberKey: string): Observable<BadgeProgressDto[]> {
    return this.http.get<BadgeProgressDto[]>(`${this.apiUrl}/${memberKey}/badges/progress`);
  }

  submitBadgeProgress(
    memberKey: string,
    badgeId: string,
    request: SubmitBadgeProgressRequestDto
  ): Observable<BadgeProgressDto> {
    return this.http.post<BadgeProgressDto>(
      `${this.apiUrl}/${memberKey}/badges/${badgeId}/submit`,
      request
    );
  }

  reviewBadgeProgress(
    memberKey: string,
    badgeId: string,
    request: ReviewBadgeProgressRequestDto
  ): Observable<BadgeProgressDto> {
    return this.http.post<BadgeProgressDto>(
      `${this.apiUrl}/${memberKey}/badges/${badgeId}/review`,
      request
    );
  }

  getProbeProgress(memberKey: string, probeId: string): Observable<ProbeProgressDto> {
    return this.http.get<ProbeProgressDto>(`${this.apiUrl}/${memberKey}/probes/${probeId}/progress`);
  }

  updateProbeProgressStatus(
    memberKey: string,
    probeId: string,
    request: UpdateProbeProgressStatusRequestDto
  ): Observable<ProbeProgressDto> {
    return this.http.put<ProbeProgressDto>(
      `${this.apiUrl}/${memberKey}/probes/${probeId}/progress/status`,
      request
    );
  }

  signProbePoint(
    memberKey: string,
    probeId: string,
    pointId: string,
    request: UpdateProbePointSignatureRequestDto
  ): Observable<ProbeProgressDto> {
    return this.http.put<ProbeProgressDto>(
      `${this.apiUrl}/${memberKey}/probes/${probeId}/points/${pointId}/sign`,
      request
    );
  }

  unsignProbePoint(
    memberKey: string,
    probeId: string,
    pointId: string,
    request: UpdateProbePointSignatureRequestDto
  ): Observable<ProbeProgressDto> {
    return this.http.put<ProbeProgressDto>(
      `${this.apiUrl}/${memberKey}/probes/${probeId}/points/${pointId}/unsign`,
      request
    );
  }
}