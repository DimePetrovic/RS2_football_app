import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { PlayerBadgeComponent } from '../../../../shared/components/player-badge/player-badge.component';
import { ProfileService } from '../../services/profile.service';
import { ProfileSearchResult } from '../../models/profile.models';

export interface FollowListDialogData {
  userId: string;
  mode: 'followers' | 'following';
}

/** Lists the followers of a player, or the players they follow, as clickable badges. */
@Component({
  selector: 'app-follow-list-dialog',
  imports: [MatDialogModule, MatButtonModule, MatProgressSpinnerModule, TranslatePipe, PlayerBadgeComponent],
  template: `
    <h2 mat-dialog-title>
      {{ (data.mode === 'followers' ? 'profile.followersTitle' : 'profile.followingTitle') | translate }}
    </h2>
    <mat-dialog-content>
      @if (loading()) {
        <div class="follow-list__center"><mat-spinner diameter="32" /></div>
      } @else if (players().length === 0) {
        <p class="follow-list__empty">
          {{ (data.mode === 'followers' ? 'profile.noFollowers' : 'profile.noFollowing') | translate }}
        </p>
      } @else {
        <ul class="follow-list">
          @for (p of players(); track p.userId) {
            <li (click)="close()">
              <app-player-badge size="sm"
                [userId]="p.userId" [avatarUrl]="p.avatarUrl"
                [username]="p.username" [name]="p.firstName + ' ' + p.lastName" [countryCode]="p.nationality" />
            </li>
          }
        </ul>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">{{ 'common.close' | translate }}</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .follow-list {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: 10px;
      min-width: 260px;
      max-height: 50vh;
      overflow-y: auto;
    }
    .follow-list__center { display: flex; justify-content: center; padding: 24px 0; }
    .follow-list__empty { margin: 0; color: var(--cb-text-muted); }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FollowListDialogComponent implements OnInit {
  private readonly profileService = inject(ProfileService);
  private readonly dialogRef = inject(MatDialogRef<FollowListDialogComponent>);
  readonly data = inject<FollowListDialogData>(MAT_DIALOG_DATA);

  readonly loading = signal(true);
  readonly players = signal<ProfileSearchResult[]>([]);

  ngOnInit() {
    const source = this.data.mode === 'followers'
      ? this.profileService.getFollowers(this.data.userId)
      : this.profileService.getFollowingOf(this.data.userId);
    source.subscribe({
      next: players => { this.players.set(players); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  close() {
    this.dialogRef.close();
  }
}
