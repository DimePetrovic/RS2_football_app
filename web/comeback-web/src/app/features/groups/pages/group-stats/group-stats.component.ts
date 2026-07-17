import {
  ChangeDetectionStrategy, Component, OnInit, computed, inject, signal,
} from '@angular/core';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatchService } from '../../../matches/services/match.service';
import { GroupService } from '../../services/group.service';
import { GroupOpponentStat, GroupStatsResponse } from '../../../matches/models/match.models';

@Component({
  selector: 'app-group-stats',
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './group-stats.component.html',
  styleUrl: './group-stats.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupStatsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly matchService = inject(MatchService);
  private readonly groupService = inject(GroupService);

  private readonly groupId = this.route.snapshot.paramMap.get('groupId')!;

  readonly loading = signal(true);
  readonly error = signal(false);
  readonly stats = signal<GroupStatsResponse | null>(null);
  readonly groupName = signal('');

  readonly winRate = computed(() => this.percent(this.stats()?.wins ?? 0, this.stats()?.playedCount ?? 0));
  readonly drawRate = computed(() => this.percent(this.stats()?.draws ?? 0, this.stats()?.playedCount ?? 0));
  readonly lossRate = computed(() => this.percent(this.stats()?.losses ?? 0, this.stats()?.playedCount ?? 0));

  readonly mostPlayed = computed(() => this.top(o => o.played));
  readonly mostBeaten = computed(() => this.top(o => o.wins));
  readonly mostLostTo = computed(() => this.top(o => o.losses));

  ngOnInit() {
    this.matchService.getGroupStats(this.groupId).subscribe({
      next: (s) => { this.stats.set(s); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
    this.groupService.getGroupById(this.groupId).subscribe({
      next: (g) => this.groupName.set(g.name),
    });
  }

  goBack() { history.back(); }

  private top(metric: (o: GroupOpponentStat) => number): GroupOpponentStat[] {
    return [...(this.stats()?.opponents ?? [])]
      .filter(o => metric(o) > 0)
      .sort((a, b) => metric(b) - metric(a))
      .slice(0, 3);
  }

  private percent(part: number, whole: number): number {
    return whole === 0 ? 0 : Math.round((part / whole) * 100);
  }
}
