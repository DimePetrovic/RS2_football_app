import { ChangeDetectionStrategy, Component, inject, signal, computed } from '@angular/core';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { MatchService } from '../../services/match.service';
import { MatchDetailResponse } from '../../models/match.models';
import { StepperComponent } from '../../../../shared/components/stepper/stepper.component';

export interface EditMatchDialogData {
  match: MatchDetailResponse;
}

@Component({
  selector: 'app-edit-match-dialog',
  imports: [
    MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatDatepickerModule, MatCheckboxModule, MatIconModule, TranslatePipe,
    StepperComponent,
  ],
  templateUrl: './edit-match-dialog.component.html',
  styleUrl: './edit-match-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditMatchDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<EditMatchDialogComponent>);
  private readonly matchService = inject(MatchService);
  private readonly i18n = inject(TranslationService);
  readonly data = inject<EditMatchDialogData>(MAT_DIALOG_DATA);

  private readonly startsAt = new Date(this.data.match.startsAt);

  readonly title = signal(this.data.match.title);
  readonly location = signal(this.data.match.location ?? '');
  readonly matchDate = signal<Date | null>(this.startsAt);
  readonly matchHour = signal<number | null>(this.startsAt.getHours());
  readonly matchMinute = signal<number | null>(
    this.startsAt.getMinutes() >= 30 ? 30 : 0
  );
  readonly durationUnknown = signal(this.data.match.durationMinutes === null);
  readonly durationMinutes = signal(this.data.match.durationMinutes ?? 120);

  readonly minDate = new Date();
  readonly hourOptions = Array.from({ length: 24 }, (_, i) => i);
  readonly minuteOptions = [0, 30];

  readonly submitting = signal(false);
  readonly error = signal('');

  readonly steps = ['match.edit.stepBasic', 'match.edit.stepSchedule'].map(k => this.i18n.translate(k));
  readonly currentStep = signal(0);

  step1Valid(): boolean { return !!this.title().trim() && !!this.location().trim(); }

  next() { if (this.step1Valid()) this.currentStep.set(1); }
  prev() { this.currentStep.set(0); }
  goToStep(i: number) { if (i < this.currentStep()) this.currentStep.set(i); }

  readonly isValid = computed(() => {
    if (!this.title().trim() || !this.location().trim() || !this.matchDate()) return false;
    if (this.matchHour() === null || this.matchMinute() === null) return false;
    if (!this.durationUnknown() && this.durationMinutes() <= 0) return false;
    const d = new Date(this.matchDate()!);
    d.setHours(this.matchHour()!, this.matchMinute()!, 0, 0);
    return d > new Date();
  });

  submit() {
    if (!this.isValid() || this.submitting()) return;

    const d = new Date(this.matchDate()!);
    d.setHours(this.matchHour()!, this.matchMinute()!, 0, 0);

    this.submitting.set(true);
    this.error.set('');

    this.matchService.updateMatchDetails(this.data.match.id, {
      title: this.title().trim(),
      location: this.location().trim(),
      startsAt: d.toISOString(),
      durationMinutes: this.durationUnknown() ? null : this.durationMinutes(),
    }).subscribe({
      next: () => { this.submitting.set(false); this.dialogRef.close(true); },
      error: () => {
        this.submitting.set(false);
        this.error.set(this.i18n.translate('match.edit.error'));
      },
    });
  }

  cancel() { this.dialogRef.close(false); }
}
