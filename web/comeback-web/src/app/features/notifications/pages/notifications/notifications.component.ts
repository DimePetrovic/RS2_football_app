import { ChangeDetectionStrategy, Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import { NotificationService } from '../../services/notification.service';
import { NotificationResponse } from '../../models/notification.models';
import { NotificationPresenterService, PresentedNotification } from '../../services/notification-presenter.service';
import { TranslationService } from '../../../../core/i18n/translation.service';

interface DisplayNotification extends NotificationResponse, PresentedNotification {}

@Component({
  selector: 'app-notifications',
  imports: [DatePipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsComponent implements OnInit {
  private readonly notificationService = inject(NotificationService);
  private readonly presenter = inject(NotificationPresenterService);
  private readonly i18n = inject(TranslationService);
  private readonly router = inject(Router);

  readonly notifications = signal<DisplayNotification[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly hasUnread = computed(() => this.notifications().some(n => !n.isRead));

  t(key: string): string {
    return this.i18n.translate(key);
  }

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set(false);
    this.notificationService.getNotifications().subscribe({
      next: (n) => {
        this.notifications.set(n.map(item => ({ ...item, ...this.presenter.present(item) })));
        this.loading.set(false);
        this.notificationService.syncCount(n);
      },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  markRead(n: NotificationResponse) {
    if (n.isRead) {
      this.handleNotificationAction(n);
      return;
    }
    this.notificationService.markRead(n.id).subscribe({
      next: () => {
        this.notifications.update(list =>
          list.map(item => item.id === n.id ? { ...item, isRead: true } : item));
        this.handleNotificationAction(n);
      },
    });
  }

  markAllRead() {
    this.notificationService.markAllRead().subscribe({
      next: () => {
        this.notifications.update(list => list.map(item => ({ ...item, isRead: true })));
      },
    });
  }

  private handleNotificationAction(n: NotificationResponse) {
    if (!n.payload) return;
    try {
      const payload = JSON.parse(n.payload);
      if (payload.matchId) {
        const queryParams = n.type === 'MatchResultSubmitted' ? { result: '1' } : undefined;
        this.router.navigate(['/matches', payload.matchId], { queryParams });
      }
    } catch { /* ignore invalid payload */ }
  }
}
