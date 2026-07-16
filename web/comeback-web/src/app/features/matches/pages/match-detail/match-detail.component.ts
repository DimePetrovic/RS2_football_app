import {
  ChangeDetectionStrategy, Component, OnInit, inject, signal, computed
} from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatchStatusPipe } from '../../../../shared/pipes/match-status.pipe';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatMenuModule } from '@angular/material/menu';
import { MatSelectModule } from '@angular/material/select';
import { MatDialog } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';
import { MatchService } from '../../services/match.service';
import { ToastService } from '../../../../core/notifications/toast.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { PlayerBadgeComponent } from '../../../../shared/components/player-badge/player-badge.component';
import { MatchMediaComponent } from '../../components/match-media/match-media.component';
import { MatchResultFormComponent } from '../../components/match-result-form/match-result-form.component';
import {
  MatchDetailResponse, MatchReviewResponse, ParticipantResponse,
} from '../../models/match.models';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import {
  MatchOutcome,
  MatchResultSummaryData,
  MatchResultSummaryDialogComponent,
} from '../../components/match-result-summary-dialog/match-result-summary-dialog.component';
import {
  PlayerReviewDialogComponent,
  PlayerReviewDialogData,
} from '../../components/player-review-dialog/player-review-dialog.component';
import {
  EditMatchDialogComponent,
  EditMatchDialogData,
} from '../../components/edit-match-dialog/edit-match-dialog.component';
import {
  InvitePlayersDialogComponent,
  InvitePlayersDialogData,
} from '../../components/invite-players-dialog/invite-players-dialog.component';

@Component({
  selector: 'app-match-detail',
  imports: [
    MatTooltipModule,
    DatePipe, FormsModule, RouterLink, TranslatePipe, MatchStatusPipe,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDividerModule,
    MatSelectModule, MatMenuModule, PlayerBadgeComponent, MatchMediaComponent, MatchResultFormComponent,
  ],
  templateUrl: './match-detail.component.html',
  styleUrl: './match-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MatchDetailComponent implements OnInit {
  private readonly matchService = inject(MatchService);
  private readonly i18n = inject(TranslationService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  protected matchId = this.route.snapshot.paramMap.get('id')!;

  readonly match = signal<MatchDetailResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly actionLoading = signal(false);

  readonly showCaptainSelector = signal(false);
  readonly homeCaptainId = signal('');
  readonly awayCaptainId = signal('');

  readonly currentUserId = computed(() => this.auth.currentUser()?.userId ?? '');
  readonly isOrganizer = computed(() =>
    this.match()?.organizerUserId === this.currentUserId() ||
    this.match()?.secondOrganizerUserId === this.currentUserId());

  readonly canRespondToGroupInvite = computed(() => {
    const m = this.match();
    return !!m && m.type === 'GroupVsGroup' && m.status === 'Scheduled' &&
      m.opponentGroupInviteStatus === 'Pending' &&
      m.opponentGroupCaptainUserId === this.currentUserId();
  });

  readonly myParticipant = computed(() =>
    this.match()?.participants.find(p => p.userId === this.currentUserId()));

  readonly canRespond = computed(() =>
    this.myParticipant()?.status === 'Invited' && this.match()?.status === 'Scheduled');
  readonly canWithdraw = computed(() =>
    this.myParticipant()?.status === 'Accepted' && !this.myParticipant()?.isOrganizer &&
    this.match()?.secondOrganizerUserId !== this.currentUserId());
  readonly canSubmitResult = computed(() => {
    const s = this.match()?.status;
    return this.isOrganizer() && (s === 'Scheduled' || s === 'ResultOverdue');
  });

  readonly requiredPlayers = computed(() => ((this.match()?.playersPerTeam ?? 0) + 1) * 2);
  readonly missingPlayers = computed(() =>
    Math.max(0, this.requiredPlayers() - this.acceptedParticipants().length));
  readonly enoughPlayersForResult = computed(() => this.missingPlayers() <= 2);

  readonly matchEndsAt = computed(() => {
    const m = this.match();
    if (!m) return null;
    const duration = m.durationMinutes ?? 120;
    return new Date(new Date(m.startsAt).getTime() + duration * 60000);
  });
  readonly matchTimeOver = computed(() => {
    const end = this.matchEndsAt();
    return !!end && Date.now() >= end.getTime();
  });

  readonly resultFormEnabled = computed(() => this.matchTimeOver() && this.enoughPlayersForResult());
  readonly canCancel = computed(() =>
    this.isOrganizer() && this.match()?.status === 'Scheduled');
  readonly canAssignTeams = computed(() => {
    const s = this.match()?.status;
    return this.isOrganizer() && (s === 'Scheduled' || s === 'ResultOverdue');
  });
  readonly canEditMatch = computed(() =>
    this.isOrganizer() && this.match()?.status === 'Scheduled');

  readonly acceptedParticipants = computed(() =>
    this.match()?.participants.filter(p => p.status === 'Accepted') ?? []);
  readonly homeTeam = computed(() =>
    this.acceptedParticipants().filter(p => p.team === 'Home'));
  readonly awayTeam = computed(() =>
    this.acceptedParticipants().filter(p => p.team === 'Away'));
  readonly unassigned = computed(() =>
    this.acceptedParticipants().filter(p => p.team === 'None'));
  readonly teamsAssigned = computed(() =>
    this.acceptedParticipants().length > 0 && this.unassigned().length === 0);
  readonly otherParticipants = computed(() =>
    this.match()?.participants.filter(p => p.status !== 'Accepted') ?? []);

  readonly eligibleScorers = computed(() =>
    this.acceptedParticipants().filter(p => p.team !== 'None'));

  // Reviews
  readonly reviews = signal<MatchReviewResponse[]>([]);
  readonly canReview = computed(() => {
    const m = this.match();
    if (!m || m.status !== 'ResultSubmitted') return false;
    const me = this.acceptedParticipants().find(p => p.userId === this.currentUserId());
    return !!me && me.team !== 'None';
  });
  readonly myParticipantId = computed(() =>
    this.acceptedParticipants().find(p => p.userId === this.currentUserId())?.id ?? '');
  readonly reviewablePlayers = computed(() =>
    this.acceptedParticipants().filter(
      p => p.team !== 'None' && p.userId !== this.currentUserId() && !p.isGuest
    )
  );
  readonly myReviewedIds = computed(() =>
    new Set(
      this.reviews()
        .filter(r => r.reviewerParticipantId === this.myParticipantId())
        .map(r => r.reviewedParticipantId)
    )
  );
  readonly unreviewedPlayers = computed(() =>
    this.reviewablePlayers().filter(p => !this.myReviewedIds().has(p.id))
  );
  readonly allReviewed = computed(() =>
    this.reviewablePlayers().length > 0 && this.unreviewedPlayers().length === 0
  );
  readonly matchEndsAtFormatted = computed(() => {
    const end = this.matchEndsAt();
    if (!end) return '';
    const d = end.getDate();
    const m = end.getMonth() + 1;
    const h = String(end.getHours()).padStart(2, '0');
    const min = String(end.getMinutes()).padStart(2, '0');
    return `${d}.${m}. ${h}:${min}`;
  });

  // Match media (loaded and managed by the child app-match-media).
  readonly canAddMedia = computed(() => {
    const m = this.match();
    return this.myParticipant()?.status === 'Accepted' &&
      !!m && new Date(m.startsAt).getTime() <= Date.now() &&
      m.status !== 'Cancelled';
  });

  readonly canRequestPlayers = computed(() =>
    this.isOrganizer() && this.match()?.status === 'Scheduled');

  requestPlayers(position: string | null) {
    const id = this.match()?.id;
    if (!id) return;
    this.actionLoading.set(true);
    this.matchService.requestPlayers(id, position).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.toast.success(this.i18n.translate('match.detail.playerRequested'));
      },
      error: () => this.actionLoading.set(false),
    });
  }

  ngOnInit() {
    // The route is reused when navigating match -> match (e.g. via a notification),
    // so react to param changes instead of loading once.
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (!id) return;
      this.matchId = id;
      this.match.set(null);
      this.load();
    });
  }

  load() {
    this.loading.set(true);
    this.error.set(false);
    this.matchService.getMatch(this.matchId).subscribe({
      next: (m) => {
        this.match.set(m);
        this.loading.set(false);
        if (m.status === 'ResultSubmitted') this.loadReviews();
        this.maybeShowResultSummary(m);
      },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  private maybeShowResultSummary(m: MatchDetailResponse) {
    if (this.route.snapshot.queryParamMap.get('result') !== '1') return;
    this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });

    const me = m.participants.find(p => p.userId === this.currentUserId());
    if (!me || (me.team !== 'Home' && me.team !== 'Away') || m.homeScore === null || m.awayScore === null) return;

    const isDraw = m.homeScore === m.awayScore;
    const winnerTeam: 'Home' | 'Away' = m.homeScore > m.awayScore ? 'Home' : 'Away';
    const outcome: MatchOutcome = isDraw ? 'draw' : (me.team === winnerTeam ? 'win' : 'loss');

    // The backend computes XP (single source of truth — MatchXpRules); here it is only displayed.
    const xpChange = m.myXpChange ?? 0;

    const canReview = m.participants.some(
      p => p.userId !== this.currentUserId() && p.status === 'Accepted',
    );

    const ref = this.dialog.open(MatchResultSummaryDialogComponent, {
      width: '360px',
      data: { outcome, homeScore: m.homeScore, awayScore: m.awayScore, xpChange, canReview } satisfies MatchResultSummaryData,
    });

    ref.afterClosed().subscribe(result => {
      if (result === 'review') this.openReviewDialog();
    });
  }

  loadReviews() {
    this.matchService.getReviews(this.matchId).subscribe({
      next: (r) => this.reviews.set(r),
    });
  }

  openReviewDialog() {
    const players = this.unreviewedPlayers();
    if (players.length === 0) return;
    const ref = this.dialog.open(PlayerReviewDialogComponent, {
      width: '440px',
      disableClose: true,
      data: { players, matchId: this.matchId } satisfies PlayerReviewDialogData,
    });
    ref.afterClosed().subscribe(() => this.loadReviews());
  }

  accept() {
    this.actionLoading.set(true);
    this.matchService.respondToInvitation(this.matchId, true).subscribe({
      next: () => { this.actionLoading.set(false); this.load(); },
      error: () => this.actionLoading.set(false),
    });
  }

  decline() {
    this.actionLoading.set(true);
    this.matchService.respondToInvitation(this.matchId, false).subscribe({
      next: () => { this.actionLoading.set(false); this.load(); },
      error: () => this.actionLoading.set(false),
    });
  }

  respondToGroupInvite(accept: boolean) {
    this.actionLoading.set(true);
    this.matchService.respondToGroupInvite(this.matchId, accept).subscribe({
      next: () => { this.actionLoading.set(false); this.load(); },
      error: () => this.actionLoading.set(false),
    });
  }

  withdraw() {
    this.actionLoading.set(true);
    this.matchService.withdraw(this.matchId).subscribe({
      next: () => { this.actionLoading.set(false); this.load(); },
      error: () => this.actionLoading.set(false),
    });
  }

  cancelMatch() {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      width: '360px',
      data: {
        titleKey: 'match.cancelDialog.title',
        messageKey: 'match.cancelDialog.message',
        confirmLabelKey: 'match.cancelDialog.confirm',
        confirmColor: 'warn',
      },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.actionLoading.set(true);
      this.matchService.cancelMatch(this.matchId).subscribe({
        next: () => { this.actionLoading.set(false); this.router.navigate(['/matches']); },
        error: () => this.actionLoading.set(false),
      });
    });
  }

  assignToTeam(p: ParticipantResponse, team: 'Home' | 'Away' | 'None') {
    this.matchService.assignToTeam(this.matchId, p.userId, team).subscribe({
      next: () => this.load(),
    });
  }

  kickParticipant(p: ParticipantResponse) {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      width: '360px',
      data: {
        titleKey: 'match.detail.kickDialog.title',
        messageKey: 'match.detail.kickDialog.message',
        confirmLabelKey: 'match.detail.kickDialog.confirm',
        confirmColor: 'warn',
      },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.matchService.removeParticipant(this.matchId, p.userId).subscribe({
        next: () => this.load(),
      });
    });
  }

  randomize() {
    this.actionLoading.set(true);
    this.matchService.randomizeTeams(this.matchId).subscribe({
      next: () => { this.actionLoading.set(false); this.load(); },
      error: () => this.actionLoading.set(false),
    });
  }

  randomizeWithCaptains() {
    if (!this.homeCaptainId() || !this.awayCaptainId()) return;
    this.actionLoading.set(true);
    this.matchService.randomizeTeamsWithCaptains(
      this.matchId, this.homeCaptainId(), this.awayCaptainId()
    ).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.showCaptainSelector.set(false);
        this.load();
      },
      error: () => this.actionLoading.set(false),
    });
  }

  balance() {
    this.actionLoading.set(true);
    this.matchService.balanceTeams(this.matchId).subscribe({
      next: () => { this.actionLoading.set(false); this.load(); },
      error: () => this.actionLoading.set(false),
    });
  }

  openEditDialog() {
    const m = this.match();
    if (!m) return;
    const ref = this.dialog.open(EditMatchDialogComponent, {
      width: '420px',
      data: { match: m } satisfies EditMatchDialogData,
    });
    ref.afterClosed().subscribe((saved: boolean) => {
      if (saved) this.load();
    });
  }

  openInviteDialog() {
    const m = this.match();
    if (!m) return;
    const ref = this.dialog.open(InvitePlayersDialogComponent, {
      width: '420px',
      data: {
        matchId: this.matchId,
        existingUserIds: m.participants.map(p => p.userId),
      } satisfies InvitePlayersDialogData,
    });
    ref.afterClosed().subscribe((invited: boolean) => {
      if (invited) this.load();
    });
  }

  goBack() { history.back(); }

  participantStatusKey(status: string): string {
    return 'match.participant.status.' + status.toLowerCase();
  }

}
