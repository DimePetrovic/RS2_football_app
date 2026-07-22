import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { NotificationResponse, UnreadCountResponse } from '../models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  readonly unreadCount = signal(0);

  loadUnreadCount() {
    this.http.get<UnreadCountResponse>(`${this.base}/api/notifications/unread-count`).subscribe({
      next: (res) => this.unreadCount.set(res.count),
    });
  }

  getNotifications() {
    return this.http.get<NotificationResponse[]>(`${this.base}/api/notifications`);
  }

  markRead(id: string) {
    return this.http.put<void>(`${this.base}/api/notifications/${id}/read`, {}).pipe(
      tap(() => this.unreadCount.update(n => Math.max(0, n - 1)))
    );
  }

  markAllRead() {
    return this.http.put<void>(`${this.base}/api/notifications/read-all`, {}).pipe(
      tap(() => this.unreadCount.set(0))
    );
  }

  syncCount(notifications: NotificationResponse[]) {
    this.unreadCount.set(notifications.filter(n => !n.isRead).length);
  }
}
