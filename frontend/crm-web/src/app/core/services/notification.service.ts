import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, interval, of } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface NotificationItem {
  notificationId: number;
  userId?: number;
  targetRole?: string;
  title: string;
  message: string;
  type: string;
  leadId?: number;
  isRead: boolean;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private apiUrl = `${environment.apiUrl}/notifications`;

  private notificationsSubject = new BehaviorSubject<NotificationItem[]>([]);
  public notifications$ = this.notificationsSubject.asObservable();

  private unreadCountSubject = new BehaviorSubject<number>(0);
  public unreadCount$ = this.unreadCountSubject.asObservable();

  private pollingStarted = false;

  constructor(private http: HttpClient) {}

  public initPolling(): void {
    if (this.pollingStarted) return;
    this.pollingStarted = true;

    // Initial load
    this.refresh();

    // Poll every 25 seconds
    interval(25000)
      .pipe(
        switchMap(() => this.fetchNotifications().pipe(catchError(() => of([])))),
        tap((items) => {
          this.notificationsSubject.next(items);
          const unread = items.filter(n => !n.isRead).length;
          this.unreadCountSubject.next(unread);
        })
      )
      .subscribe();
  }

  public refresh(): void {
    this.fetchNotifications().subscribe({
      next: (items) => {
        this.notificationsSubject.next(items);
        const unread = items.filter(n => !n.isRead).length;
        this.unreadCountSubject.next(unread);
      },
      error: () => {}
    });
  }

  public fetchNotifications(limit: number = 30): Observable<NotificationItem[]> {
    return this.http.get<NotificationItem[]>(`${this.apiUrl}?limit=${limit}`);
  }

  public markAsRead(notificationId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${notificationId}/read`, {}).pipe(
      tap(() => {
        const current = this.notificationsSubject.value;
        const updated = current.map(item => 
          item.notificationId === notificationId ? { ...item, isRead: true } : item
        );
        this.notificationsSubject.next(updated);
        this.unreadCountSubject.next(updated.filter(n => !n.isRead).length);
      })
    );
  }

  public markAllAsRead(): Observable<any> {
    return this.http.post(`${this.apiUrl}/read-all`, {}).pipe(
      tap(() => {
        const current = this.notificationsSubject.value;
        const updated = current.map(item => ({ ...item, isRead: true }));
        this.notificationsSubject.next(updated);
        this.unreadCountSubject.next(0);
      })
    );
  }

  public clearNotification(notificationId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${notificationId}`).pipe(
      tap(() => {
        const current = this.notificationsSubject.value.filter(n => n.notificationId !== notificationId);
        this.notificationsSubject.next(current);
        this.unreadCountSubject.next(current.filter(n => !n.isRead).length);
      })
    );
  }

  public clearAll(): Observable<any> {
    return this.http.delete(`${this.apiUrl}/clear-all`).pipe(
      tap(() => {
        this.notificationsSubject.next([]);
        this.unreadCountSubject.next(0);
      })
    );
  }
}
