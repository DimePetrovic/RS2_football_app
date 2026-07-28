import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../../core/auth/auth.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { errorMessage } from '../../../../core/notifications/error.interceptor';
import { StepperComponent } from '../../../../shared/components/stepper/stepper.component';
import { COUNTRY_CODES, flagClass } from '../../../../core/countries/countries';

type PageState = 'validating' | 'form' | 'loading' | 'expired' | 'resending' | 'resent';

const GOALKEEPER_POSITION = 0;
const CURRENT_YEAR = new Date().getFullYear();
const MIN_YEAR = 1920;
const MIN_AGE = 5;

@Component({
  selector: 'app-complete-profile',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TranslatePipe,
    StepperComponent,
  ],
  templateUrl: './complete-profile.component.html',
  styleUrl: './complete-profile.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompleteProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly i18n = inject(TranslationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly pageState = signal<PageState>('validating');
  readonly errorMessage = signal<string | null>(null);
  readonly selectedPosition = signal<number | null>(null);
  readonly isGoalkeeper = computed(() => this.selectedPosition() === GOALKEEPER_POSITION);

  private userId = '';
  private token = '';

  readonly minDob = new Date(MIN_YEAR, 0, 1);
  readonly maxDob = new Date(CURRENT_YEAR - MIN_AGE, 11, 31);

  readonly positions = [
    { value: 0, labelKey: 'auth.completeProfile.positions.goalkeeper' },
    { value: 1, labelKey: 'auth.completeProfile.positions.defender' },
    { value: 2, labelKey: 'auth.completeProfile.positions.midfielder' },
    { value: 3, labelKey: 'auth.completeProfile.positions.forward' },
  ];

  readonly form = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    dateOfBirth: [null as Date | null, Validators.required],
    nationality: [null as string | null],
    preferredPosition: [null as number | null, Validators.required],
    canPlayGoalkeeper: [false],
    youthSeasons: [0, [Validators.required, Validators.min(0), Validators.max(10)]],
    seniorSeasons: [0, [Validators.required, Validators.min(0), Validators.max(20)]],
  });

  readonly countryOptions = computed(() =>
    COUNTRY_CODES
      .map(code => ({ code, name: this.i18n.translate(`countries.${code}`), flag: flagClass(code) }))
      .sort((a, b) => a.name.localeCompare(b.name, 'sr')));

  readonly resendForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
  });

  readonly steps = ['auth.completeProfile.stepData', 'auth.completeProfile.stepFootball']
    .map(k => this.i18n.translate(k));
  readonly currentStep = signal(0);

  step1Valid(): boolean {
    return !!this.form.get('firstName')?.valid
      && !!this.form.get('lastName')?.valid
      && !!this.form.get('dateOfBirth')?.valid;
  }

  next() {
    if (this.step1Valid()) {
      this.currentStep.set(1);
    } else {
      this.form.get('firstName')?.markAsTouched();
      this.form.get('lastName')?.markAsTouched();
      this.form.get('dateOfBirth')?.markAsTouched();
    }
  }

  prev() { this.currentStep.set(0); }

  goToStep(i: number) { if (i < this.currentStep()) this.currentStep.set(i); }

  ngOnInit() {
    this.userId = this.route.snapshot.queryParamMap.get('userId') ?? '';
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';

    if (!this.userId || !this.token) {
      this.pageState.set('expired');
      return;
    }

    this.auth.validateEmailToken(this.userId, this.token).subscribe({
      next: (res) => this.pageState.set(res.isValid ? 'form' : 'expired'),
      error: () => this.pageState.set('expired'),
    });

    this.form.get('preferredPosition')!.valueChanges.subscribe((val) => {
      this.selectedPosition.set(val);
      if (val === GOALKEEPER_POSITION) {
        this.form.get('canPlayGoalkeeper')!.setValue(true);
      }
    });
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.pageState.set('loading');
    this.errorMessage.set(null);

    const { firstName, lastName, dateOfBirth, preferredPosition, canPlayGoalkeeper, youthSeasons, seniorSeasons } =
      this.form.value;

    const dob = dateOfBirth!;
    const isoDate = `${dob.getFullYear()}-${String(dob.getMonth() + 1).padStart(2, '0')}-${String(dob.getDate()).padStart(2, '0')}`;

    this.auth.completeRegistration({
      userId: this.userId,
      token: this.token,
      firstName: firstName!,
      lastName: lastName!,
      dateOfBirth: isoDate,
      preferredPosition: preferredPosition!,
      canPlayGoalkeeper: canPlayGoalkeeper ?? false,
      youthSeasons: youthSeasons ?? 0,
      seniorSeasons: seniorSeasons ?? 0,
      nationality: this.form.value.nationality ?? null,
    }).subscribe({
      next: () => this.router.navigate(['/feed']),
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(errorMessage(err, this.i18n));
        this.pageState.set('form');
      },
    });
  }

  resend() {
    if (this.resendForm.invalid) {
      this.resendForm.markAllAsTouched();
      return;
    }

    this.pageState.set('resending');

    this.auth.resendConfirmation({ email: this.resendForm.value.email! }).subscribe({
      next: () => this.pageState.set('resent'),
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(errorMessage(err, this.i18n));
        this.pageState.set('expired');
      },
    });
  }
}
