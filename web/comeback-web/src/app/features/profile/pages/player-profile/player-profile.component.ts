import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { MatDialog } from '@angular/material/dialog';
import { ReviewsSectionComponent } from '../../components/reviews-section/reviews-section.component';
import { FollowListDialogComponent } from '../../components/follow-list-dialog/follow-list-dialog.component';
import { flagClass } from '../../../../core/countries/countries';
import { MatchHistorySectionComponent } from '../../components/match-history-section/match-history-section.component';
import { ProfileService } from '../../services/profile.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { RatingService } from '../../../../core/rating/rating.service';
import { MatchService } from '../../../matches/services/match.service';
import { FollowCounts, ProfileResponse } from '../../models/profile.models';
import { PlayerXpData } from '../../../../core/rating/rating.models';
import { PlayerReceivedReviewItem, PlayerMatchHistoryItem } from '../../../matches/models/match.models';

@Component({
  selector: 'app-player-profile',
  imports: [
    MatTooltipModule,
    RouterLink,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, TranslatePipe,
    ReviewsSectionComponent, MatchHistorySectionComponent,
  ],
  templateUrl: './player-profile.component.html',
  styleUrl: './player-profile.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlayerProfileComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly profileService = inject(ProfileService);
  private readonly ratingService = inject(RatingService);
  private readonly matchService = inject(MatchService);

  readonly loading = signal(true);
  readonly error = signal(false);
  readonly profile = signal<ProfileResponse | null>(null);
  readonly xp = signal<PlayerXpData | null>(null);
  readonly reviews = signal<PlayerReceivedReviewItem[]>([]);
  readonly matchHistory = signal<PlayerMatchHistoryItem[]>([]);
  readonly isFollowing = signal(false);
  readonly followLoading = signal(false);
  readonly followCounts = signal<FollowCounts | null>(null);
  readonly nationalityFlag = computed(() => flagClass(this.profile()?.nationality));


  readonly xpProgressPercent = computed(() => {
    const x = this.xp();
    if (!x) return 0;
    const currentLevelXp = 400 * Math.pow(x.level - 1, 2);
    const nextLevelXp = 400 * Math.pow(x.level, 2);
    const range = nextLevelXp - currentLevelXp;
    if (range <= 0) return 100;
    return Math.round(((x.totalXp - currentLevelXp) / range) * 100);
  });

  readonly avgOverall = computed(() => avg(this.reviews().map(r => r.overallRating)));
  readonly avgGoalkeeping = computed(() => avg(this.reviews().map(r => r.goalkeepingRating).filter(notNull)));
  readonly avgDefense = computed(() => avg(this.reviews().map(r => r.defenseRating).filter(notNull)));
  readonly avgAttack = computed(() => avg(this.reviews().map(r => r.attackRating).filter(notNull)));
  readonly avgEffort = computed(() => avg(this.reviews().map(r => r.effortRating).filter(notNull)));

  private get userId(): string {
    return this.route.snapshot.paramMap.get('userId')!;
  }

  readonly isOwnProfile = computed(() => this.auth.currentUser()?.userId === this.userId);

  navigateToEdit() {
    this.router.navigate(['/profile', 'edit']);
  }

  ngOnInit() {
    // The route is reused when navigating between profiles (e.g. another player's
    // profile -> own profile via the nav bar), so react to param changes, not just init.
    this.route.paramMap.subscribe((params) => {
      const uid = params.get('userId');
      if (uid) this.load(uid);
    });
  }

  private load(uid: string) {
    this.loading.set(true);
    this.error.set(false);
    this.profile.set(null);
    this.xp.set(null);
    this.reviews.set([]);
    this.matchHistory.set([]);
    this.followCounts.set(null);
    this.isFollowing.set(false);

    this.profileService.getProfile(uid).subscribe({
      next: (p) => {
        this.profile.set(p);
        this.loading.set(false);
        this.ratingService.getPlayerProfile(p.userId).subscribe({
          next: (bff) => this.xp.set(bff.rating),
          error: () => {},
        });
        this.matchService.getPlayerReceivedReviews(uid).subscribe({
          next: (r) => this.reviews.set(r),
          error: () => {},
        });
        this.matchService.getPlayerMatchHistory(uid).subscribe({
          next: (h) => this.matchHistory.set(h),
          error: () => {},
        });
        this.loadFollowCounts(uid);
        if (!this.isOwnProfile()) {
          this.profileService.getFollowStatus(uid).subscribe({
            next: (res) => this.isFollowing.set(res.isFollowing),
            error: () => {},
          });
        }
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }


  goBack() { history.back(); }

  private loadFollowCounts(userId: string) {
    this.profileService.getFollowCounts(userId).subscribe({
      next: counts => this.followCounts.set(counts),
      error: () => {},
    });
  }

  openFollowList(mode: 'followers' | 'following') {
    const p = this.profile();
    if (!p) return;
    this.dialog.open(FollowListDialogComponent, {
      width: '340px',
      data: { userId: p.userId, mode },
    });
  }

  toggleFollow() {
    const p = this.profile();
    if (!p || this.followLoading()) return;
    this.followLoading.set(true);

    const action$ = this.isFollowing()
      ? this.profileService.unfollow(p.userId)
      : this.profileService.follow(p.userId);

    action$.subscribe({
      next: () => {
        this.isFollowing.set(!this.isFollowing());
        this.followLoading.set(false);
        this.loadFollowCounts(p.userId);
      },
      error: () => this.followLoading.set(false),
    });
  }

  startChat() {
    const p = this.profile();
    if (!p) return;
    this.router.navigate(['/chats'], {
      queryParams: { userId: p.userId, name: p.username },
    });
  }
}

function avg(vals: number[]): number | null {
  if (!vals.length) return null;
  return +( vals.reduce((s, v) => s + v, 0) / vals.length).toFixed(1);
}

function notNull<T>(v: T | null): v is T {
  return v !== null;
}
