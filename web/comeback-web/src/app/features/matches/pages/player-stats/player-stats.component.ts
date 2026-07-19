import {
  ChangeDetectionStrategy, Component, OnInit, computed, inject, signal,
} from '@angular/core';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { AuthService } from '../../../../core/auth/auth.service';
import { MatchService } from '../../services/match.service';
import { GroupService } from '../../../groups/services/group.service';
import { ProfileSearchResult } from '../../../profile/models/profile.models';
import {
  PlayedWithRelation, PlayerMatchHistoryItem, PlayerStatsResponse,
} from '../../models/match.models';
import {
  StatsChartBucket, StatsChartComponent,
} from '../../../../shared/components/stats-chart/stats-chart.component';
import { PlayerBadgeComponent } from '../../../../shared/components/player-badge/player-badge.component';
import { PlayerSearchFieldComponent } from '../../../../shared/components/player-search-field/player-search-field.component';

type ChartPeriod = 'week' | 'month' | 'year';

const MONTH_LABELS = ['jan', 'feb', 'mar', 'apr', 'maj', 'jun', 'jul', 'avg', 'sep', 'okt', 'nov', 'dec'];

@Component({
  selector: 'app-player-stats',
  imports: [
    DatePipe, RouterLink,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatFormFieldModule, MatInputModule, MatAutocompleteModule,
    StatsChartComponent, PlayerBadgeComponent, TranslatePipe, PlayerSearchFieldComponent,
  ],
  templateUrl: './player-stats.component.html',
  styleUrl: './player-stats.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlayerStatsComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly matchService = inject(MatchService);
  private readonly groupService = inject(GroupService);

  private readonly userId = this.auth.currentUser()?.userId ?? '';

  readonly loading = signal(true);
  readonly error = signal(false);
  readonly stats = signal<PlayerStatsResponse | null>(null);
  readonly groupCount = signal<number | null>(null);

  readonly period = signal<ChartPeriod>('month');

  // ── Izvedeni procenti ─────────────────────────────────────────
  readonly organizeRate = computed(() => this.percent(
    this.stats()?.organizedWithResult ?? 0, this.stats()?.organizedCount ?? 0));
  readonly winRate = computed(() => this.percent(this.stats()?.wins ?? 0, this.stats()?.playedCount ?? 0));
  readonly drawRate = computed(() => this.percent(this.stats()?.draws ?? 0, this.stats()?.playedCount ?? 0));
  readonly lossRate = computed(() => this.percent(this.stats()?.losses ?? 0, this.stats()?.playedCount ?? 0));

  readonly topGroup = computed(() => this.stats()?.groupsPlayedWith[0] ?? null);

  // ── Chart: buckets by the selected period ────────────────────
  readonly chartBuckets = computed<StatsChartBucket[]>(() => {
    const timeline = this.stats()?.timeline ?? [];
    const now = new Date();
    const period = this.period();

    const outcomes = (items: typeof timeline) => ({
      wins: items.filter(t => t.outcome === 'Win').length,
      draws: items.filter(t => t.outcome === 'Draw').length,
      losses: items.filter(t => t.outcome === 'Loss').length,
    });

    if (period === 'year') {
      const buckets: StatsChartBucket[] = [];
      for (let i = 11; i >= 0; i--) {
        const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
        const items = timeline.filter(t => {
          const s = new Date(t.startsAt);
          return s.getFullYear() === d.getFullYear() && s.getMonth() === d.getMonth();
        });
        buckets.push({
          label: `${MONTH_LABELS[d.getMonth()]} ${String(d.getFullYear()).slice(2)}`,
          ...outcomes(items),
        });
      }
      return buckets;
    }

    const days = period === 'week' ? 7 : 30;
    const buckets: StatsChartBucket[] = [];
    for (let i = days - 1; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth(), now.getDate() - i);
      const items = timeline.filter(t => {
        const s = new Date(t.startsAt);
        return s.getFullYear() === d.getFullYear()
            && s.getMonth() === d.getMonth()
            && s.getDate() === d.getDate();
      });
      buckets.push({
        label: `${d.getDate()}.${d.getMonth() + 1}.`,
        ...outcomes(items),
      });
    }
    return buckets;
  });

  // ── Search: matches with the selected player ─────────────────────
  readonly selectedPlayer = signal<ProfileSearchResult | null>(null);
  readonly searchExcludedIds = computed(() => [this.userId]);
  readonly relation = signal<PlayedWithRelation>('All');
  readonly filteredMatches = signal<PlayerMatchHistoryItem[] | null>(null);
  readonly searchLoading = signal(false);


  constructor() {
  }

  ngOnInit() {
    this.matchService.getPlayerStats(this.userId).subscribe({
      next: (s) => { this.stats.set(s); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
    this.groupService.getMyGroups().subscribe({
      next: (groups) => this.groupCount.set(groups.length),
    });
  }

  selectPlayer(p: ProfileSearchResult) {
    this.selectedPlayer.set(p);
    this.loadFilteredMatches();
  }

  clearPlayer() {
    this.selectedPlayer.set(null);
    this.filteredMatches.set(null);
  }

  setRelation(r: PlayedWithRelation) {
    this.relation.set(r);
    if (this.selectedPlayer()) this.loadFilteredMatches();
  }

  private loadFilteredMatches() {
    const player = this.selectedPlayer();
    if (!player) return;
    this.searchLoading.set(true);
    this.matchService.getPlayedWithMatches(this.userId, player.userId, this.relation()).subscribe({
      next: (items) => { this.filteredMatches.set(items); this.searchLoading.set(false); },
      error: () => { this.filteredMatches.set([]); this.searchLoading.set(false); },
    });
  }

  outcomeFor(m: PlayerMatchHistoryItem): 'win' | 'draw' | 'loss' | 'none' {
    if (m.homeScore === null || m.awayScore === null || m.team === 'None') return 'none';
    if (m.homeScore === m.awayScore) return 'draw';
    const winner = m.homeScore > m.awayScore ? 'Home' : 'Away';
    return m.team === winner ? 'win' : 'loss';
  }

  goBack() { history.back(); }

  private percent(part: number, whole: number): number {
    return whole === 0 ? 0 : Math.round((part / whole) * 100);
  }
}
