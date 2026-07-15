import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { AuthService } from '../../../../core/auth/auth.service';

type ConfirmState = 'verifying' | 'success' | 'error';

@Component({
  selector: 'app-confirm-email',
  imports: [RouterLink, MatButtonModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './confirm-email.component.html',
  styleUrl: './confirm-email.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmEmailComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly state = signal<ConfirmState>('verifying');

  ngOnInit() {
    const userId = this.route.snapshot.queryParamMap.get('userId');
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!userId || !token) {
      this.state.set('error');
      return;
    }

    this.auth.validateEmailToken(userId, token).subscribe({
      next: (res) => {
        if (res.isValid) {
          this.state.set('success');
          setTimeout(() => this.router.navigate(['/auth/complete-registration'], {
            queryParams: { userId, token },
          }), 2500);
        } else {
          this.state.set('error');
        }
      },
      error: () => this.state.set('error'),
    });
  }
}
