import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../../core/auth/auth.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { errorMessage } from '../../../../core/notifications/error.interceptor';
import { StepperComponent } from '../../../../shared/components/stepper/stepper.component';

type PageState = 'form' | 'success' | 'loading';

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TranslatePipe,
    StepperComponent,
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly i18n = inject(TranslationService);
  private readonly fb = inject(FormBuilder);

  readonly pageState = signal<PageState>('form');
  readonly errorMessage = signal<string | null>(null);
  readonly registeredEmail = signal<string>('');
  readonly passwordVisible = signal(false);

  readonly steps = ['auth.register.stepAccount', 'auth.register.password'].map(k => this.i18n.translate(k));
  readonly currentStep = signal(0);

  readonly form = this.fb.group(
    {
      email: ['', [Validators.required, Validators.email]],
      username: ['', [Validators.required, Validators.minLength(3)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: this.passwordMatchValidator }
  );

  step1Valid(): boolean {
    return !!this.form.get('email')?.valid && !!this.form.get('username')?.valid;
  }

  next() {
    if (this.step1Valid()) {
      this.currentStep.set(1);
    } else {
      this.form.get('email')?.markAsTouched();
      this.form.get('username')?.markAsTouched();
    }
  }

  prev() { this.currentStep.set(0); }

  goToStep(i: number) { if (i < this.currentStep()) this.currentStep.set(i); }

  togglePassword() {
    this.passwordVisible.update((v) => !v);
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.pageState.set('loading');
    this.errorMessage.set(null);

    const { email, username, password, confirmPassword } = this.form.value;

    this.auth
      .register({ email: email!, username: username!, password: password!, confirmPassword: confirmPassword! })
      .subscribe({
        next: () => {
          this.registeredEmail.set(email!);
          this.pageState.set('success');
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(errorMessage(err, this.i18n));
          this.pageState.set('form');
        },
      });
  }

  private passwordMatchValidator(group: import('@angular/forms').AbstractControl) {
    const pw = group.get('password')?.value;
    const cpw = group.get('confirmPassword')?.value;
    return pw === cpw ? null : { passwordMismatch: true };
  }
}
