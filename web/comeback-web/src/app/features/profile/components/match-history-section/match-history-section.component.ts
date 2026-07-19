import { ChangeDetectionStrategy, Component, Input, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { MatchStatusPipe } from '../../../../shared/pipes/match-status.pipe';
import { PlayerMatchHistoryItem } from '../../../matches/models/match.models';

const COLLAPSED_COUNT = 5;

/** The "Matches" profile section: played-match history with status/score/team tags. */
@Component({
  selector: 'app-match-history-section',
  imports: [DatePipe, RouterLink, MatButtonModule, TranslatePipe, MatchStatusPipe],
  templateUrl: './match-history-section.component.html',
  styleUrl: './match-history-section.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MatchHistorySectionComponent {
  private readonly i18n = inject(TranslationService);
  private readonly matchesSignal = signal<PlayerMatchHistoryItem[]>([]);

  @Input({ required: true }) set matches(value: PlayerMatchHistoryItem[]) {
    this.matchesSignal.set(value ?? []);
  }
  get matches(): PlayerMatchHistoryItem[] { return this.matchesSignal(); }

  readonly showAll = signal(false);
  readonly all = computed(() => this.matchesSignal());
  readonly visible = computed(() =>
    this.showAll() ? this.all() : this.all().slice(0, COLLAPSED_COUNT));
  readonly collapsible = computed(() => this.all().length > COLLAPSED_COUNT);

  teamLabel(team: string): string {
    return team === 'Home'
      ? this.i18n.translate('profile.teamHome')
      : team === 'Away' ? this.i18n.translate('profile.teamAway') : '';
  }
}
