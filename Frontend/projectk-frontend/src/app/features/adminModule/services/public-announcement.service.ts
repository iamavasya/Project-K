import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  PublicAnnouncementCleanupStatus,
  PublicAnnouncementDraft,
  PublicAnnouncementDraftRequest,
  PublicAnnouncementImageUploadResult,
  PublicAnnouncementPreview,
  PublicAnnouncementStatus
} from '../models/public-announcement.model';

@Injectable({
  providedIn: 'root'
})
export class PublicAnnouncementService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/admin/public-announcements`;

  getDrafts(status?: PublicAnnouncementStatus | null): Observable<PublicAnnouncementDraft[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<PublicAnnouncementDraft[]>(this.apiUrl, { params });
  }

  getCleanupStatus(): Observable<PublicAnnouncementCleanupStatus> {
    return this.http.get<PublicAnnouncementCleanupStatus>(`${this.apiUrl}/cleanup-status`);
  }

  createDraft(request: PublicAnnouncementDraftRequest): Observable<PublicAnnouncementDraft> {
    return this.http.post<PublicAnnouncementDraft>(this.apiUrl, request);
  }

  uploadImage(file: File): Observable<PublicAnnouncementImageUploadResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<PublicAnnouncementImageUploadResult>(`${this.apiUrl}/image`, formData);
  }

  deleteImage(imageKey: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/image/${encodeURIComponent(imageKey)}`);
  }

  updateDraft(draftKey: string, request: PublicAnnouncementDraftRequest): Observable<PublicAnnouncementDraft> {
    return this.http.put<PublicAnnouncementDraft>(`${this.apiUrl}/${draftKey}`, request);
  }

  previewDraft(draftKey: string): Observable<PublicAnnouncementPreview> {
    return this.http.post<PublicAnnouncementPreview>(`${this.apiUrl}/${draftKey}/preview`, {});
  }

  submitDraft(draftKey: string): Observable<PublicAnnouncementDraft> {
    return this.http.post<PublicAnnouncementDraft>(`${this.apiUrl}/${draftKey}/submit`, {});
  }

  approveDraft(draftKey: string): Observable<PublicAnnouncementDraft> {
    return this.http.post<PublicAnnouncementDraft>(`${this.apiUrl}/${draftKey}/approve`, {});
  }

  rejectDraft(draftKey: string): Observable<PublicAnnouncementDraft> {
    return this.http.post<PublicAnnouncementDraft>(`${this.apiUrl}/${draftKey}/reject`, {});
  }

  publishDraft(draftKey: string): Observable<PublicAnnouncementDraft> {
    return this.http.post<PublicAnnouncementDraft>(`${this.apiUrl}/${draftKey}/publish`, {});
  }

  deleteDraft(draftKey: string): Observable<PublicAnnouncementDraft> {
    return this.http.delete<PublicAnnouncementDraft>(`${this.apiUrl}/${draftKey}`);
  }
}
