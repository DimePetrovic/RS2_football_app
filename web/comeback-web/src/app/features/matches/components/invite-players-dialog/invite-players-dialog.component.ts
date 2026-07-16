import { ChangeDetectionStrategy, Component, inject, signal, computed } from '@angular/core';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { MatchService } from '../../services/match.service';
import { ProfileSearchResult } from '../../../profile/models/profile.models';
import { PlayerSearchFieldComponent } from '../../../../shared/components/player-search-field/player-search-field.component';
import { CreateMatchInviteeDto } from '../../models/match.models';

export interface InvitePlayersDialogData {
  matchId: string;
  existingUserIds: string[];
}

@Component({
  selector: 'app-invite-players-dialog',
  imports: [
    MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule,
    MatIconModule, TranslatePipe, PlayerSearchFieldComponent,
  ],
  templateUrl: './invite-players-dialog.component.html',
  styleUrl: './invite-players-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InvitePlayersDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<InvitePlayersDialogComponent>);
  private readonly matchService = inject(MatchService);
  private readonly i18n = inject(TranslationService);
  readonly data = inject<InvitePlayersDialogData>(MAT_DIALOG_DATA);

  readonly invitees = signal<CreateMatchInviteeDto[]>([]);
  readonly guestNames = signal<string[]>([]);
  readonly guestInput = signal('');
  readonly submitting = signal(false);
  readonly error = signal('');

  readonly excludedIds = computed(() => [
    ...this.data.existingUserIds,
    ...this.invitees().map(i => i.userId),
  ]);

  readonly isValid = computed(() => this.invitees().length > 0 || this.guestNames().length > 0);

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

  submit() {
    if (!this.isValid() || this.submitting()) return;

    this.submitting.set(true);
    this.error.set('');

    this.matchService.inviteAdditionalPlayers(this.data.matchId, {
      invitees: this.invitees(),
      guestNames: this.guestNames(),
    }).subscribe({
      next: () => { this.submitting.set(false); this.dialogRef.close(true); },
      error: () => {
        this.submitting.set(false);
        this.error.set(this.i18n.translate('match.invite.error'));
      },
    });
  }

  cancel() { this.dialogRef.close(false); }
}
