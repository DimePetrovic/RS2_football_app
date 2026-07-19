import { ChangeDetectionStrategy, Component, inject, signal, computed } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { MatchService } from '../../services/match.service';
import { ParticipantResponse } from '../../models/match.models';

export interface PlayerReviewDialogData {
  players: ParticipantResponse[];
  matchId: string;
}

@Component({
  selector: 'app-player-review-dialog',
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './player-review-dialog.component.html',
  styleUrl: './player-review-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlayerReviewDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<PlayerReviewDialogComponent>);
  private readonly matchService = inject(MatchService);
  readonly data = inject<PlayerReviewDialogData>(MAT_DIALOG_DATA);

  readonly ratingValues = [5, 5.5, 6, 6.5, 7, 7.5, 8, 8.5, 9, 9.5, 10];
  readonly specs = [
    { key: 'goalkeeping', labelKey: 'match.review.goalkeeping' },
    { key: 'defense',     labelKey: 'match.review.defense' },
    { key: 'attack',      labelKey: 'match.review.attack' },
    { key: 'effort',      labelKey: 'match.review.effort' },
  ];

  readonly currentIndex = signal(0);
  readonly overallRating = signal<number | null>(null);
  readonly specRatings = signal<Record<string, number | null>>({});
  readonly comment = signal('');
  readonly submitting = signal(false);

  readonly currentPlayer = computed(() => this.data.players[this.currentIndex()]);
  readonly isLast = computed(() => this.currentIndex() === this.data.players.length - 1);
  readonly canSubmit = computed(() => this.overallRating() !== null && !this.submitting());

  initials(name: string): string {
    const parts = name.trim().split(/\s+/);
    return parts.length >= 2
      ? (parts[0][0] + parts[1][0]).toUpperCase()
      : name.substring(0, 2).toUpperCase();
  }

  getSpec(key: string): number | null | undefined {
    const r = this.specRatings();
    return key in r ? r[key] : undefined;
  }

  setSpec(key: string, val: number | null) {
    this.specRatings.update(r => ({ ...r, [key]: val }));
  }

  fmt(val: number): string {
    return val % 1 === 0 ? String(val) : val.toFixed(1);
  }

  private resetForm() {
    this.overallRating.set(null);
    this.specRatings.set({});
    this.comment.set('');
  }

  private advance() {
    if (this.isLast()) {
      this.dialogRef.close(true);
    } else {
      this.currentIndex.update(i => i + 1);
      this.resetForm();
    }
  }

  submit() {
    const overall = this.overallRating();
    if (overall === null) return;
    const specs = this.specRatings();
    this.submitting.set(true);
    this.matchService.submitReview(this.data.matchId, {
      reviewedParticipantId: this.currentPlayer().id,
      overallRating: overall,
      goalkeepingRating: 'goalkeeping' in specs ? specs['goalkeeping'] : undefined,
      defenseRating:     'defense'     in specs ? specs['defense']     : undefined,
      attackRating:      'attack'      in specs ? specs['attack']      : undefined,
      effortRating:      'effort'      in specs ? specs['effort']      : undefined,
      comment: this.comment() || null,
    }).subscribe({
      next: () => { this.submitting.set(false); this.advance(); },
      error: () => this.submitting.set(false),
    });
  }

  skip()   { this.advance(); }
  cancel() { this.dialogRef.close(false); }
}
