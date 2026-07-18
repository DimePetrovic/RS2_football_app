import {
  ChangeDetectionStrategy, Component, EventEmitter, Input, Output, computed, inject, signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { errorMessage } from '../../../../core/notifications/error.interceptor';
import { MatchService } from '../../services/match.service';
import { GoalEntryRequest, ParticipantResponse } from '../../models/match.models';

interface GoalFormRow {
  scorerUserId: string;
  isOwnGoal: boolean;
  assistUserId: string | null;
}

/**
 * Match result entry: score + individual goals (scorer, own goal, assist).
 * Notifies the parent (submitted) to reload the match after a successful entry.
 */
@Component({
  selector: 'app-match-result-form',
  imports: [MatButtonModule, MatIconModule, MatSelectModule, MatCheckboxModule, TranslatePipe],
  templateUrl: './match-result-form.component.html',
  styleUrl: './match-result-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MatchResultFormComponent {
  private readonly matchService = inject(MatchService);
  private readonly i18n = inject(TranslationService);

  @Input({ required: true }) matchId!: string;
  @Input() eligibleScorers: ParticipantResponse[] = [];
  @Input() enabled = false;
  @Input() timeOver = false;
  @Input() enoughPlayers = false;
  @Input() endsAtFormatted = '';
  @Input() missingPlayers = 0;

  @Output() submitted = new EventEmitter<void>();

  readonly showForm = signal(false);
  readonly submitting = signal(false);
  readonly homeScore = signal(0);
  readonly awayScore = signal(0);
  readonly error = signal<string | null>(null);
  readonly goalEntries = signal<GoalFormRow[]>([]);

  private participantByUserId(userId: string): ParticipantResponse | undefined {
    return this.eligibleScorers.find(p => p.userId === userId);
  }

  scoringTeamFor(row: GoalFormRow): 'Home' | 'Away' | null {
    const scorer = this.participantByUserId(row.scorerUserId);
    if (!scorer || scorer.team === 'None') return null;
    if (!row.isOwnGoal) return scorer.team as 'Home' | 'Away';
    return scorer.team === 'Home' ? 'Away' : 'Home';
  }

  assistOptionsFor(row: GoalFormRow): ParticipantResponse[] {
    const scorer = this.participantByUserId(row.scorerUserId);
    if (!scorer || scorer.team === 'None') return [];
    return this.eligibleScorers.filter(p => p.team === scorer.team && p.userId !== row.scorerUserId);
  }

  readonly homeGoalsEntered = computed(() =>
    this.goalEntries().filter(g => this.scoringTeamFor(g) === 'Home').length);
  readonly awayGoalsEntered = computed(() =>
    this.goalEntries().filter(g => this.scoringTeamFor(g) === 'Away').length);

  readonly goalsMatchScore = computed(() =>
    this.homeGoalsEntered() === this.homeScore() && this.awayGoalsEntered() === this.awayScore());

  readonly allGoalsHaveScorer = computed(() =>
    this.goalEntries().every(g => !!g.scorerUserId));

  readonly canConfirm = computed(() =>
    this.goalsMatchScore() && this.allGoalsHaveScorer() && !this.submitting());

  open() {
    this.goalEntries.set([]);
    this.showForm.set(true);
  }

  close() { this.showForm.set(false); }

  addGoalEntry() {
    const first = this.eligibleScorers[0];
    this.goalEntries.update(rows => [
      ...rows,
      { scorerUserId: first?.userId ?? '', isOwnGoal: false, assistUserId: null },
    ]);
  }

  removeGoalEntry(index: number) {
    this.goalEntries.update(rows => rows.filter((_, i) => i !== index));
  }

  updateGoalEntry(index: number, patch: Partial<GoalFormRow>) {
    this.goalEntries.update(rows => rows.map((r, i) => {
      if (i !== index) return r;
      const updated = { ...r, ...patch };
      if (updated.isOwnGoal) updated.assistUserId = null;
      return updated;
    }));
  }

  submit() {
    this.submitting.set(true);
    this.error.set(null);
    const goals: GoalEntryRequest[] = this.goalEntries().map(g => ({
      scorerUserId: g.scorerUserId,
      isOwnGoal: g.isOwnGoal,
      assistUserId: g.assistUserId,
    }));
    this.matchService.submitResult(this.matchId, {
      homeScore: this.homeScore(),
      awayScore: this.awayScore(),
      goals,
    }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.showForm.set(false);
        this.submitted.emit();
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.error.set(errorMessage(err, this.i18n));
      },
    });
  }
}
