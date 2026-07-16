import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';

export type MatchOutcome = 'win' | 'loss' | 'draw';

export interface MatchResultSummaryData {
  outcome: MatchOutcome;
  homeScore: number;
  awayScore: number;
  xpChange: number;
  canReview?: boolean;
}

@Component({
  selector: 'app-match-result-summary-dialog',
  imports: [MatDialogModule, MatButtonModule, MatIconModule, TranslatePipe],
  template: `
    <div class="result-summary"
      [class.result-summary--win]="data.outcome === 'win'"
      [class.result-summary--loss]="data.outcome === 'loss'"
      [class.result-summary--draw]="data.outcome === 'draw'">
      <mat-icon class="result-summary__icon">
        {{ data.outcome === 'win' ? 'emoji_events' : data.outcome === 'loss' ? 'sentiment_dissatisfied' : 'handshake' }}
      </mat-icon>
      <h2 mat-dialog-title class="result-summary__title">
        {{ ('match.result.' + data.outcome) | translate }}
      </h2>
      <p class="result-summary__score">{{ data.homeScore }} : {{ data.awayScore }}</p>
      <div class="result-summary__xp">
        <mat-icon>bolt</mat-icon>
        <span>+{{ data.xpChange }} XP</span>
      </div>
      <p class="result-summary__rating-note">{{ 'match.result.ratingNote' | translate }}</p>
    </div>
    <mat-dialog-actions align="end">
      <button mat-flat-button [mat-dialog-close]="false">{{ 'common.close' | translate }}</button>
      @if (data.canReview) {
        <button mat-flat-button color="primary" [mat-dialog-close]="'review'">{{ 'match.result.reviewPlayers' | translate }}</button>
      }
    </mat-dialog-actions>
  `,
  styles: [`
    .result-summary {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      padding: 8px 16px;
      gap: 4px;
    }
    .result-summary__icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      margin-bottom: 4px;
    }
    .result-summary--win .result-summary__icon,
    .result-summary--win .result-summary__title { color: var(--cb-success); }
    .result-summary--loss .result-summary__icon,
    .result-summary--loss .result-summary__title { color: var(--cb-error); }
    .result-summary--draw .result-summary__icon,
    .result-summary--draw .result-summary__title { color: var(--cb-text-muted); }
    .result-summary__title { margin: 0; }
    .result-summary__score {
      font-family: 'Oswald', sans-serif;
      font-size: 28px;
      font-weight: 700;
      margin: 4px 0 12px;
    }
    .result-summary__xp {
      display: flex;
      align-items: center;
      gap: 4px;
      color: var(--cb-primary);
      font-weight: 700;
      font-size: 16px;
    }
    .result-summary__rating-note {
      margin: 12px 0 0;
      font-size: 12px;
      color: var(--cb-text-muted);
    }
    mat-dialog-actions { gap: 8px; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MatchResultSummaryDialogComponent {
  readonly data = inject<MatchResultSummaryData>(MAT_DIALOG_DATA);
}
