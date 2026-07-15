import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject } from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { AuthService } from '../../core/auth/auth.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { LogoComponent } from '../../shared/components/logo/logo.component';
import { NotificationService } from '../../features/notifications/services/notification.service';
import { NotificationHubService } from '../../features/notifications/services/notification-hub.service';
import { ChatService } from '../../features/chat/services/chat.service';
import { ChatHubService } from '../../features/chat/services/chat-hub.service';

interface NavItem {
  labelKey: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-main-layout',
  imports: [
    MatTooltipModule,RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe, MatIconModule, MatButtonModule, LogoComponent],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  private readonly dialog = inject(MatDialog);
  private readonly notificationService = inject(NotificationService);
  private readonly notificationHub = inject(NotificationHubService);
  private readonly chatService = inject(ChatService);
  private readonly chatHub = inject(ChatHubService);
  readonly auth = inject(AuthService);

  readonly unreadCount = computed(() => this.notificationService.unreadCount());
  readonly unreadChatCount = computed(() => this.chatService.unreadCount());

  readonly navItems: NavItem[] = [
    { labelKey: 'nav.feed',    icon: 'dynamic_feed',  route: '/feed' },
    { labelKey: 'nav.search',  icon: 'search',        route: '/search' },
    { labelKey: 'nav.matches', icon: 'sports_soccer', route: '/matches' },
    { labelKey: 'nav.groups',  icon: 'groups',        route: '/groups' },
    { labelKey: 'nav.profile', icon: 'person',        route: '/profile' },
  ];

  ngOnInit() {
    this.notificationService.loadUnreadCount();
    this.chatService.loadUnreadCount();
    this.notificationHub.connect();
    this.chatHub.connect();
  }

  ngOnDestroy() {
    this.notificationHub.disconnect();
    this.chatHub.disconnect();
  }

  confirmLogout(): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      width: '320px',
      data: { titleKey: 'logout.title', messageKey: 'logout.message', confirmLabelKey: 'nav.logout' },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) this.auth.logout();
    });
  }
}
