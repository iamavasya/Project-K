import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';

/** What the person wrote, already as Markdown, plus where they were. */
export interface ProblemReportPayload {
  title: string | null;
  description: string;
  steps: string | null;
  expected: string | null;
  route: string;
  appVersion: string;
  screenshotUrls: string[];
}

/** Where the report went; both null when the server only logged it. */
export interface ProblemReportReceipt {
  issueUrl: string | null;
  issueNumber: number | null;
}

/**
 * «Повідомити про проблему». Screenshots go up first, one by one, and come back as addresses the
 * report embeds; the report itself is one request. GitHub is the server's business.
 */
@Injectable({ providedIn: 'root' })
export class FeedbackService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/feedback`;

  uploadScreenshot(file: File): Observable<{ url: string }> {
    const form = new FormData();
    form.append('file', file, file.name || 'screenshot.png');
    return this.http.post<{ url: string }>(`${this.apiUrl}/screenshots`, form);
  }

  reportProblem(payload: ProblemReportPayload): Observable<ProblemReportReceipt> {
    return this.http.post<ProblemReportReceipt>(`${this.apiUrl}/problems`, payload);
  }
}
