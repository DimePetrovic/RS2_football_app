import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { ActivatedRoute, Router } from '@angular/router';
import { RouterLink } from '@angular/router';
import { PlayerBadgeComponent } from '../../../../shared/components/player-badge/player-badge.component';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { GroupService } from '../../services/group.service';
import { GroupDetail, GroupMember } from '../../models/group.models';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { AddMemberDialogComponent } from '../../components/add-member-dialog/add-member-dialog.component';
import { ProfileSearchResult } from '../../../profile/models/profile.models';
import { EditGroupDialogComponent } from '../../components/edit-group-dialog/edit-group-dialog.component';
import { MatchService } from '../../../matches/services/match.service';
import { MatchSummaryResponse } from '../../../matches/models/match.models';

@Component({
  selector: 'app-group-detail',
  imports: [
    MatTooltipModule,RouterLink, DatePipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule, PlayerBadgeComponent, TranslatePipe],
  templateUrl: './group-detail.component.html',
  styleUrl: './group-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly groupService = inject(GroupService);
  private readonly matchService = inject(MatchService);
  private readonly dialog = inject(MatDialog);
  private readonly i18n = inject(TranslationService);

  readonly group = signal<GroupDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly matchHistory = signal<MatchSummaryResponse[]>([]);

  private get groupId(): string {
    return this.route.snapshot.paramMap.get('groupId')!;
  }

  ngOnInit() {
    this.load();
    this.matchService.getGroupMatchHistory(this.groupId).subscribe({
      next: (matches) => this.matchHistory.set(matches),
      error: () => {},
    });
  }

  openAddMember() {
    const ref = this.dialog.open(AddMemberDialogComponent, {
      width: '360px',
      data: { existingMembers: this.group()!.members },
    });
    ref.afterClosed().subscribe((player: ProfileSearchResult | undefined) => {
      if (!player) return;
      this.groupService.addMember(this.groupId, player.userId).subscribe({
        next: () => this.load(),
      });
    });
  }

  openEdit() {
    const g = this.group()!;
    const ref = this.dialog.open(EditGroupDialogComponent, {
      width: '360px',
      data: { groupId: g.id, name: g.name, avatarUrl: g.avatarUrl },
    });
    ref.afterClosed().subscribe((updated: boolean | undefined) => {
      if (updated) this.load();
    });
  }

  confirmRemoveMember(member: GroupMember) {
    const name = member.displayName ?? `${member.firstName} ${member.lastName}`;
    const ref = this.dialog.open(ConfirmDialogComponent, {
      width: '320px',
      data: {
        titleKey: 'groups.removeMember.title',
        messageKey: 'groups.removeMember.message',
        messageParams: { name },
        confirmLabelKey: 'groups.removeMember.confirm',
      },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) this.removeMember(member);
    });
  }

  confirmLeave() {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      width: '320px',
      data: {
        titleKey: 'groups.leave.title',
        messageKey: 'groups.leave.message',
        confirmLabelKey: 'groups.leave.confirm',
      },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) this.leave();
    });
  }

  goBack() {
    this.router.navigate(['/groups']);
  }

  load() {
    this.loading.set(true);
    this.error.set(false);
    this.groupService.getGroupById(this.groupId).subscribe({
      next: (g) => { this.group.set(g); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  private removeMember(member: GroupMember) {
    this.groupService.removeMember(this.groupId, member.userId).subscribe({
      next: () => this.load(),
    });
  }

  private leave() {
    this.groupService.leaveGroup(this.groupId).subscribe({
      next: () => this.router.navigate(['/groups']),
    });
  }
}
