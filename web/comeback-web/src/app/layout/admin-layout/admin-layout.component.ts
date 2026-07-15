import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterOutlet } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { AuthService } from '../../core/auth/auth.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { LogoComponent } from '../../shared/components/logo/logo.component';

@Component({
  selector: 'app-admin-layout',
  imports: [
    MatTooltipModule,RouterOutlet, TranslatePipe, MatIconModule, MatButtonModule, LogoComponent],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLayoutComponent {
  private readonly dialog = inject(MatDialog);
  readonly auth = inject(AuthService);

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
