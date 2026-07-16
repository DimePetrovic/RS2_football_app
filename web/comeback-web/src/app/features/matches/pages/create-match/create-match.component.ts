import {
  ChangeDetectionStrategy, Component, OnInit, inject, signal, computed
} from '@angular/core';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { debounceTime, distinctUntilChanged, Subject, switchMap } from 'rxjs';
import { MatchService } from '../../services/match.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { errorMessage } from '../../../../core/notifications/error.interceptor';
import { ProfileSearchResult } from '../../../profile/models/profile.models';
import { GroupService } from '../../../groups/services/group.service';
import { GroupSearchResult, GroupSummary } from '../../../groups/models/group.models';
import { CreateMatchInviteeDto } from '../../models/match.models';
import { StepperComponent } from '../../../../shared/components/stepper/stepper.component';
import { PlayerSearchFieldComponent } from '../../../../shared/components/player-search-field/player-search-field.component';

@Component({
  selector: 'app-create-match',
  imports: [
    FormsModule, DatePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatAutocompleteModule,
    MatDatepickerModule, MatCheckboxModule,
    StepperComponent, TranslatePipe, PlayerSearchFieldComponent,
  ],
  templateUrl: './create-match.component.html',
  styleUrl: './create-match.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateMatchComponent implements OnInit {
  private readonly matchService = inject(MatchService);
  private readonly authService = inject(AuthService);
  private readonly groupService = inject(GroupService);
  private readonly router = inject(Router);
  private readonly i18n = inject(TranslationService);

  readonly title = signal('');
  readonly location = signal('');
  readonly matchDate = signal<Date | null>(null);
  readonly matchHour = signal<number | null>(null);
  readonly matchMinute = signal<number | null>(null);
  readonly hourOptions = Array.from({ length: 24 }, (_, i) => i);
  readonly minuteOptions = [0, 30];
  readonly durationMinutes = signal(60);
  readonly durationUnknown = signal(false);

  readonly minDate = new Date();
  readonly playersPerTeam = signal(5);
  readonly maxSubstitutes = signal(2);
  readonly invitees = signal<CreateMatchInviteeDto[]>([]);
  readonly guestNames = signal<string[]>([]);
  readonly guestInput = signal('');
  readonly submitting = signal(false);
  readonly error = signal('');

  // ── Wizard state ──────────────────────────────────────────────
  readonly steps = ['match.create.stepDetails', 'match.create.stepSchedule', 'match.create.stepTypePlayers', 'match.create.stepConfirm']
    .map(k => this.i18n.translate(k));
  readonly currentStep = signal(0);

  // 0 = Independent, 1 = GroupMatch, 2 = GroupVsGroup
  readonly matchType = signal<0 | 1 | 2>(0);
  readonly myGroups = signal<GroupSummary[]>([]);
  readonly selectedGroupId = signal<string | null>(null);

  readonly opponentGroupQuery = signal('');
  readonly opponentGroupResults = signal<GroupSearchResult[]>([]);
  readonly selectedOpponentGroup = signal<GroupSearchResult | null>(null);

  private readonly groupMemberIds = signal<Set<string>>(new Set());
  private readonly opponentGroupMemberIds = signal<Set<string>>(new Set());

  readonly searchExcludedIds = computed(() => [
    this.authService.currentUser()?.userId ?? '',
    ...this.invitees().map(i => i.userId),
    ...this.groupMemberIds(),
    ...this.opponentGroupMemberIds(),
  ]);

  private readonly opponentGroupSearchSubject = new Subject<string>();

  constructor() {
    this.opponentGroupSearchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(q => this.groupService.searchGroups(q, this.selectedGroupId() ?? undefined))
    ).subscribe({
      next: (results) => this.opponentGroupResults.set(results),
    });
  }

  ngOnInit() {
    this.groupService.getMyGroups().subscribe({
      next: (groups) => this.myGroups.set(groups),
    });
  }

  setMatchType(type: 0 | 1 | 2) {
    this.matchType.set(type);
    if (type === 0) {
      this.selectedGroupId.set(null);
      this.selectedOpponentGroup.set(null);
      this.groupMemberIds.set(new Set());
      this.opponentGroupMemberIds.set(new Set());
    }
  }

  onGroupSelected(groupId: string) {
    this.selectedGroupId.set(groupId);
    this.selectedOpponentGroup.set(null);
    this.opponentGroupMemberIds.set(new Set());
    this.groupService.getGroupById(groupId).subscribe({
      next: (g) => this.groupMemberIds.set(new Set(g.members.map(m => m.userId))),
    });
  }

  addInvitee(p: ProfileSearchResult) {
    this.invitees.update(list => [...list, { userId: p.userId, displayName: p.username }]);
  }

  removeInvitee(userId: string) {
    this.invitees.update(list => list.filter(i => i.userId !== userId));
  }

  addGuest() {
    const name = this.guestInput().trim();
    if (!name) return;
    this.guestNames.update(list => [...list, name]);
    this.guestInput.set('');
  }

  removeGuest(index: number) {
    this.guestNames.update(list => list.filter((_, i) => i !== index));
  }

  onOpponentGroupSearch(q: string) {
    this.opponentGroupQuery.set(q);
    if (q.length >= 2) this.opponentGroupSearchSubject.next(q);
    else this.opponentGroupResults.set([]);
  }

  selectOpponentGroup(g: GroupSearchResult) {
    this.selectedOpponentGroup.set(g);
    this.opponentGroupQuery.set('');
    this.opponentGroupResults.set([]);
    this.groupService.getGroupById(g.id).subscribe({
      next: (detail) => this.opponentGroupMemberIds.set(new Set(detail.members.map(m => m.userId))),
    });
  }

  clearOpponentGroup() {
    this.selectedOpponentGroup.set(null);
    this.opponentGroupMemberIds.set(new Set());
  }

  // ── Per-step validation ───────────────────────────────────────
  // The title is optional (gets a default value); the location is required.
  readonly detailsValid = computed(() => !!this.location().trim());

  readonly scheduleValid = computed(() => {
    if (!this.matchDate()) return false;
    if (this.matchHour() === null || this.matchMinute() === null) return false;
    if (!this.durationUnknown() && this.durationMinutes() <= 0) return false;
    if (this.playersPerTeam() <= 0) return false;

    const d = new Date(this.matchDate()!);
    d.setHours(this.matchHour()!, this.matchMinute()!, 0, 0);
    return d > new Date();
  });

  readonly playersValid = computed(() => {
    const type = this.matchType();
    if (type === 1 && !this.selectedGroupId()) return false;
    if (type === 2 && (!this.selectedGroupId() || !this.selectedOpponentGroup())) return false;
    return true;
  });

  readonly isValid = computed(() =>
    this.detailsValid() && this.scheduleValid() && this.playersValid());

  /** Whether the current step allows moving to the next one */
  readonly canProceed = computed(() => {
    switch (this.currentStep()) {
      case 0: return this.detailsValid();
      case 1: return this.scheduleValid();
      case 2: return this.playersValid();
      default: return true;
    }
  });

  /** Match title — entered or default, derived from the location. */
  readonly resolvedTitle = computed(() =>
    this.title().trim() ||
    this.i18n.translate('match.create.defaultTitle', { location: this.location().trim() }));

  // ── Summary for the "Confirm" step ─────────────────────────────
  readonly matchTypeLabel = computed(() =>
    [this.i18n.translate('match.create.typeIndependent'), this.i18n.translate('match.create.typeGroup'), this.i18n.translate('match.create.typeGroupVsGroup')][this.matchType()]);

  readonly startsAtPreview = computed(() => {
    const d = this.matchDate();
    if (!d || this.matchHour() === null || this.matchMinute() === null) return null;
    const x = new Date(d);
    x.setHours(this.matchHour()!, this.matchMinute()!, 0, 0);
    return x;
  });

  readonly selectedGroupName = computed(() =>
    this.myGroups().find(g => g.id === this.selectedGroupId())?.name ?? null);

  next() {
    if (this.canProceed() && this.currentStep() < this.steps.length - 1) {
      this.currentStep.update(s => s + 1);
    }
  }

  prev() {
    if (this.currentStep() > 0) this.currentStep.update(s => s - 1);
  }

  goToStep(i: number) {
    if (i < this.currentStep()) this.currentStep.set(i);
  }

  submit() {
    if (!this.isValid() || this.submitting()) return;

    const d = new Date(this.matchDate()!);
    d.setHours(this.matchHour()!, this.matchMinute()!, 0, 0);

    this.submitting.set(true);
    this.error.set('');

    const type = this.matchType();

    this.matchService.createMatch({
      title: this.resolvedTitle(),
      type,
      location: this.location().trim() || null,
      startsAt: d.toISOString(),
      durationMinutes: this.durationUnknown() ? null : this.durationMinutes(),
      playersPerTeam: this.playersPerTeam(),
      maxSubstitutes: this.maxSubstitutes(),
      invitees: this.invitees(),
      guestNames: this.guestNames(),
      groupId: type !== 0 ? this.selectedGroupId() : null,
      opponentGroupId: type === 2 ? this.selectedOpponentGroup()!.id : null,
    }).subscribe({
      next: (res) => {
        this.submitting.set(false);
        this.router.navigate(['/matches', res.id]);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.error.set(errorMessage(err, this.i18n));
      },
    });
  }

  goBack() {
    history.back();
  }
}
