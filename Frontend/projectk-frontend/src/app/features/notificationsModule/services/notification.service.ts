import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AppNotification } from '../models/app-notification.model';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/notifications`;
  private readonly unreadCountSubject = new BehaviorSubject<number>(0);

  readonly unreadCount$ = this.unreadCountSubject.asObservable();

  getInbox(unreadOnly = false, take = 10): Observable<AppNotification[]> {
    const params = new HttpParams()
      .set('unreadOnly', unreadOnly)
      .set('take', take);

    return this.http.get<AppNotification[]>(this.apiUrl, { params });
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/unread-count`);
  }

  refreshUnreadCount(): Observable<number> {
    return this.getUnreadCount().pipe(
      tap(count => this.unreadCountSubject.next(count))
    );
  }

  markAsRead(notificationKey: string): Observable<AppNotification> {
    return this.http.put<AppNotification>(`${this.apiUrl}/${notificationKey}/read`, {});
  }

  markAllAsRead(): Observable<number> {
    return this.http.put<number>(`${this.apiUrl}/read-all`, {}).pipe(
      tap(() => this.unreadCountSubject.next(0))
    );
  }

  setUnreadCount(count: number): void {
    this.unreadCountSubject.next(Math.max(0, count));
  }
}
