import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { FeedService } from '../../services/feed.service';
import { FeedPost, PostComment } from '../../models/feed.models';
import { PlayerBadgeComponent } from '../../../../shared/components/player-badge/player-badge.component';

@Component({
  selector: 'app-post-detail',
  imports: [PlayerBadgeComponent, 
    DatePipe, FormsModule, RouterLink,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule, TranslatePipe,
  ],
  templateUrl: './post-detail.component.html',
  styleUrl: './post-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly feedService = inject(FeedService);

  private readonly postId = this.route.snapshot.paramMap.get('id')!;

  readonly post = signal<FeedPost | null>(null);
  readonly comments = signal<PostComment[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly commentText = signal('');
  readonly submittingComment = signal(false);

  readonly homeTeam = computed(() => this.post()?.players.filter(p => p.team === 'Home') ?? []);
  readonly awayTeam = computed(() => this.post()?.players.filter(p => p.team === 'Away') ?? []);

  readonly winner = computed<'Home' | 'Away' | null>(() => {
    const p = this.post();
    if (!p) return null;
    if (p.homeScore > p.awayScore) return 'Home';
    if (p.awayScore > p.homeScore) return 'Away';
    return null;
  });

  /** Player of the match — the highest average rating in the post (min. one rating). */
  readonly motmUserId = computed<string | null>(() => {
    let best: string | null = null;
    let bestRating = -1;
    for (const p of this.post()?.players ?? []) {
      if (p.overallRating !== null && p.overallRating > bestRating) {
        bestRating = p.overallRating;
        best = p.userId;
      }
    }
    return best;
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(false);
    this.feedService.getPost(this.postId).subscribe({
      next: (post) => {
        this.post.set(post);
        this.loading.set(false);
        this.feedService.getComments(this.postId).subscribe({
          next: (comments) => this.comments.set(comments),
        });
      },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  toggleLike() {
    const post = this.post();
    if (!post) return;
    this.feedService.toggleLike(post.id).subscribe({
      next: (res) => this.post.set({
        ...post,
        likedByMe: res.liked,
        likeCount: post.likeCount + (res.liked ? 1 : -1),
      }),
    });
  }

  submitComment() {
    const content = this.commentText().trim();
    const post = this.post();
    if (!content || !post || this.submittingComment()) return;

    this.submittingComment.set(true);
    this.feedService.addComment(post.id, content).subscribe({
      next: (comment) => {
        this.comments.update(list => [...list, comment]);
        this.post.set({ ...post, commentCount: post.commentCount + 1 });
        this.commentText.set('');
        this.submittingComment.set(false);
      },
      error: () => this.submittingComment.set(false),
    });
  }

  async share() {
    try {
      await navigator.clipboard.writeText(location.href);
    } catch {
      // clipboard API unavailable — non-critical
    }
  }

  goBack() { history.back(); }
}
