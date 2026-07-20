import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { FeedService } from '../../services/feed.service';
import { FeedPost, PostPlayer } from '../../models/feed.models';
import { PlayerBadgeComponent } from '../../../../shared/components/player-badge/player-badge.component';
import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../core/notifications/toast.service';
import { MatchService } from '../../../matches/services/match.service';
import { MatDialog } from '@angular/material/dialog';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-feed',
  imports: [
    MatTooltipModule,PlayerBadgeComponent, DatePipe, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './feed.component.html',
  styleUrl: './feed.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeedComponent implements OnInit {
  private readonly feedService = inject(FeedService);
  private readonly router = inject(Router);
  private readonly i18n = inject(TranslationService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly matchService = inject(MatchService);
  private readonly dialog = inject(MatDialog);

  readonly appliedMatchIds = signal<Set<string>>(new Set());

  readonly posts = signal<FeedPost[]>([]);
  readonly loading = signal(true);
  readonly loadingMore = signal(false);
  readonly error = signal(false);
  readonly hasMore = signal(true);

  private page = 0;
  private readonly pageSize = 20;

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(false);
    this.page = 0;
    this.feedService.getFeed(this.page, this.pageSize).subscribe({
      next: (posts) => {
        this.posts.set(posts);
        this.hasMore.set(posts.length === this.pageSize);
        this.loading.set(false);
      },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  loadMore() {
    if (this.loadingMore() || !this.hasMore()) return;
    this.loadingMore.set(true);
    this.page += 1;
    this.feedService.getFeed(this.page, this.pageSize).subscribe({
      next: (posts) => {
        this.posts.update(list => [...list, ...posts]);
        this.hasMore.set(posts.length === this.pageSize);
        this.loadingMore.set(false);
      },
      error: () => this.loadingMore.set(false),
    });
  }

  toggleLike(post: FeedPost) {
    this.feedService.toggleLike(post.id).subscribe({
      next: (res) => {
        this.posts.update(list => list.map(p => p.id === post.id
          ? { ...p, likedByMe: res.liked, likeCount: p.likeCount + (res.liked ? 1 : -1) }
          : p));
      },
    });
  }

  isMyPost(post: FeedPost): boolean {
    return post.organizerUserId === (this.auth.currentUser()?.userId ?? '');
  }

  hasApplied(post: FeedPost): boolean {
    return post.viewerAlreadyIn || this.appliedMatchIds().has(post.matchId);
  }

  applyToMatch(post: FeedPost, event: Event) {
    event.stopPropagation();
    this.matchService.joinViaPublicCall(post.matchId).subscribe({
      next: () => {
        this.appliedMatchIds.update(set => new Set(set).add(post.matchId));
        this.toast.success(this.i18n.translate('feed.wanted.applied'));
      },
    });
  }

    messageOrganizer(post: FeedPost, event: Event) {
    event.stopPropagation();
    if (!post.organizerUserId) return;
    this.router.navigate(['/chats'], {
      queryParams: { userId: post.organizerUserId, name: post.organizerDisplayName ?? '' },
    });
  }

  confirmWithdraw(post: FeedPost, event: Event) {
    event.stopPropagation();
    const ref = this.dialog.open(ConfirmDialogComponent, {
      width: '360px',
      data: {
        titleKey: 'feed.wanted.withdrawDialog.title',
        messageKey: 'feed.wanted.withdrawDialog.message',
        confirmLabelKey: 'feed.wanted.withdrawDialog.confirm',
        confirmColor: 'warn',
      },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.matchService.withdraw(post.matchId).subscribe({
        next: () => {
          this.appliedMatchIds.update(set => {
            const next = new Set(set);
            next.delete(post.matchId);
            return next;
          });
          this.posts.update(list =>
            list.map(p => p.matchId === post.matchId ? { ...p, viewerAlreadyIn: false } : p));
          this.toast.success(this.i18n.translate('feed.wanted.withdrawn'));
        },
      });
    });
  }

    wantedSubject(post: FeedPost): string {
    if (!post.position) return this.i18n.translate('feed.wanted.anyPlayer');
    const key = `feed.wanted.positions.${post.position}`;
    const t = this.i18n.translate(key);
    return t === key ? this.i18n.translate('feed.wanted.anyPlayer') : t;
  }

  openMatch(post: FeedPost, event: Event) {
    event.stopPropagation();
    this.router.navigate(['/matches', post.matchId]);
  }

    openPost(post: FeedPost) {
    this.router.navigate(['/feed', post.id]);
  }

  async share(post: FeedPost, event: Event) {
    event.stopPropagation();
    const url = `${location.origin}/feed/${post.id}`;
    try {
      await navigator.clipboard.writeText(url);
    } catch {
      // clipboard API unavailable — silently ignore, link sharing is a non-critical convenience action
    }
  }

  homeTeam(post: FeedPost): PostPlayer[] {
    return post.players.filter(p => p.team === 'Home');
  }

  awayTeam(post: FeedPost): PostPlayer[] {
    return post.players.filter(p => p.team === 'Away');
  }

  winner(post: FeedPost): 'Home' | 'Away' | null {
    if (post.homeScore > post.awayScore) return 'Home';
    if (post.awayScore > post.homeScore) return 'Away';
    return null;
  }

  /** Player of the match — the highest average rating in the post (min. one rating). */
  motmUserId(post: FeedPost): string | null {
    let best: PostPlayer | null = null;
    for (const p of post.players) {
      if (p.overallRating === null) continue;
      if (!best || p.overallRating > best.overallRating!) best = p;
    }
    return best?.userId ?? null;
  }
}
