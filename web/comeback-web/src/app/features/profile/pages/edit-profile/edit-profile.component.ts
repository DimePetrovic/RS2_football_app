import {
  ChangeDetectionStrategy, Component, OnInit, computed, inject, signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { ProfileService } from '../../services/profile.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { StepperComponent } from '../../../../shared/components/stepper/stepper.component';
import { AvatarUploadComponent } from '../../../../shared/components/avatar-upload/avatar-upload.component';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { COUNTRY_CODES, flagClass } from '../../../../core/countries/countries';

const POSITIONS = ['Goalkeeper', 'Defender', 'Midfielder', 'Forward'];
const SKILL_LEVELS = ['Beginner', 'Intermediate', 'Advanced', 'Professional'];

@Component({
  selector: 'app-edit-profile',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    TranslatePipe, StepperComponent, AvatarUploadComponent,
  ],
  templateUrl: './edit-profile.component.html',
  styleUrl: './edit-profile.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditProfileComponent implements OnInit {
  private readonly profileService = inject(ProfileService);
  private readonly i18n = inject(TranslationService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly saving = signal(false);

  readonly positions = POSITIONS;
  readonly countryOptions = computed(() =>
    COUNTRY_CODES
      .map(code => ({ code, name: this.i18n.translate(`countries.${code}`), flag: flagClass(code) }))
      .sort((a, b) => a.name.localeCompare(b.name, 'sr')));
  readonly skillLevels = SKILL_LEVELS;

  readonly steps = ['Profil', 'Igra'];
  readonly currentStep = signal(0);

  readonly form = this.fb.group({
    displayName: [''],
    bio: [''],
    position: [''],
    skillLevel: [''],
    avatarUrl: [''],
    nationality: [null as string | null],
  });

  next() { this.currentStep.set(1); }
  prev() { this.currentStep.set(0); }
  goToStep(i: number) { if (i < this.currentStep()) this.currentStep.set(i); }

  ngOnInit() {
    this.profileService.getMyProfile().subscribe({
      next: (p) => {
        this.form.patchValue({
          displayName: p.displayName ?? '',
          bio: p.bio ?? '',
          position: p.preferredPosition ?? '',
          skillLevel: p.skillLevel ?? '',
          avatarUrl: p.avatarUrl ?? '',
          nationality: p.nationality ?? null,
        });
        this.loading.set(false);
      },
      error: () => this.cancel(),
    });
  }

  save() {
    this.saving.set(true);
    const val = this.form.value;
    this.profileService.updateMyProfile({
      displayName: val.displayName || null,
      bio: val.bio || null,
      position: val.position || null,
      skillLevel: val.skillLevel || null,
      avatarUrl: val.avatarUrl || null,
      nationality: val.nationality ?? null,
    }).subscribe({
      next: () => this.goToProfile(),
      error: () => this.saving.set(false),
    });
  }

  cancel() { this.goToProfile(); }

  private goToProfile() {
    const userId = this.auth.currentUser()?.userId;
    if (userId) this.router.navigate(['/players', userId]);
    else this.router.navigate(['/feed']);
  }
}
