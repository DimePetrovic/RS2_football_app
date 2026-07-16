import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatchStatusPipe } from '../../../../shared/pipes/match-status.pipe';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatchService } from '../../services/match.service';
import { MatchSummaryResponse } from '../../models/match.models';

@Component({
  selector: 'app-my-matches',
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, TranslatePipe, MatchStatusPipe],
  templateUrl: './my-matches.component.html',
  styleUrl: './my-matches.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyMatchesComponent implements OnInit {
  private readonly matchService = inject(MatchService);
  private readonly router = inject(Router);

  readonly matches = signal<MatchSummaryResponse[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set(false);
    this.matchService.getMyMatches().subscribe({
      next: (m) => { this.matches.set(m); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

}
