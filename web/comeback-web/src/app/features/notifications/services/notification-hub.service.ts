import { Injectable, inject } from '@angular/core';
import { Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from './notification.service';
import { NotificationResponse } from '../models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationHubService {
  private readonly auth = inject(AuthService);
  private readonly notificationService = inject(NotificationService);

  private hub?: HubConnection;

  readonly notificationReceived$ = new Subject<NotificationResponse>();

  connect() {
    const token = this.auth.accessToken();
    if (!token || this.hub) return;

    this.hub = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/notifications`, {
        // Factory is re-invoked on every (re)negotiate, so retries pick up a refreshed token.
        accessTokenFactory: () => this.auth.accessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.hub.on('NotificationReceived', (notification: NotificationResponse) => {
      this.notificationReceived$.next(notification);
      this.notificationService.unreadCount.update(n => n + 1);
    });

    this.startWithRetry();
  }

  // The negotiate request bypasses the HTTP interceptor, so an expired token 401s the
  // first attempt; retry with backoff — by then the proactive/interceptor refresh has run.
  private startWithRetry(attempt = 0) {
    this.hub?.start().catch(err => {
      if (this.hub && attempt < 5) {
        setTimeout(() => this.startWithRetry(attempt + 1), Math.min(2000 * (attempt + 1), 10000));
      } else {
        console.error('SignalR connection failed:', err);
      }
    });
  }

  disconnect() {
    this.hub?.stop();
    this.hub = undefined;
  }
}
