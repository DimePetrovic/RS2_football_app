import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { PlayerSearchFieldComponent } from '../../../../shared/components/player-search-field/player-search-field.component';
import { ProfileSearchResult } from '../../../profile/models/profile.models';
import { GroupMember } from '../../models/group.models';

export interface AddMemberDialogData {
  existingMembers: GroupMember[];
}

@Component({
  selector: 'app-add-member-dialog',
  imports: [MatDialogModule, MatButtonModule, TranslatePipe, PlayerSearchFieldComponent],
  template: `
    <h2 mat-dialog-title>{{ 'groups.addPlayer.title' | translate }}</h2>
    <mat-dialog-content>
      <app-player-search-field
        labelKey="groups.addPlayer.searchLabel"
        [excludedIds]="excludedIds()"
        (playerSelected)="select($event)" />
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="undefined">{{ 'common.cancel' | translate }}</button>
    </mat-dialog-actions>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddMemberDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<AddMemberDialogComponent>);
  readonly data = inject<AddMemberDialogData>(MAT_DIALOG_DATA);

  readonly excludedIds = computed(() => this.data.existingMembers.map(m => m.userId));

  select(player: ProfileSearchResult) {
    this.dialogRef.close(player);
  }
}
