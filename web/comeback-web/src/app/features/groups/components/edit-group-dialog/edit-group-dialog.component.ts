import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { GroupService } from '../../services/group.service';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { AvatarUploadComponent } from '../../../../shared/components/avatar-upload/avatar-upload.component';

export interface EditGroupDialogData {
  groupId: string;
  name: string;
  avatarUrl: string | null;
}

@Component({
  selector: 'app-edit-group-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, TranslatePipe, AvatarUploadComponent],
  template: `
    <h2 mat-dialog-title>{{ 'groups.editDialog.title' | translate }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'groups.form.name' | translate }}</mat-label>
          <input matInput formControlName="name" maxlength="100" />
        </mat-form-field>
        <app-avatar-upload
          icon="groups"
          [url]="form.controls.avatarUrl.value"
          (urlChange)="onAvatarChange($event)" />
        @if (errorMsg()) {
          <p style="font-size:12px; color:var(--cb-error); margin:4px 0 0;">{{ errorMsg() }}</p>
        }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">{{ 'common.cancel' | translate }}</button>
      <button mat-flat-button color="primary" (click)="submit()" [disabled]="saving()">{{ 'common.save' | translate }}</button>
    </mat-dialog-actions>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditGroupDialogComponent {
  private readonly groupService = inject(GroupService);
  private readonly i18n = inject(TranslationService);
  private readonly dialogRef = inject(MatDialogRef<EditGroupDialogComponent>);
  readonly data = inject<EditGroupDialogData>(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly errorMsg = signal<string | null>(null);

  readonly form = inject(FormBuilder).group({
    name: [this.data.name, [Validators.required, Validators.maxLength(100)]],
    avatarUrl: [this.data.avatarUrl ?? ''],
  });

  onAvatarChange(url: string | null) {
    this.form.controls.avatarUrl.setValue(url ?? '');
  }

  submit() {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.errorMsg.set(null);
    const val = this.form.value;
    this.groupService.updateGroup(this.data.groupId, {
      name: val.name!,
      avatarUrl: val.avatarUrl || null,
    }).subscribe({
      next: () => this.dialogRef.close(true),
      error: () => { this.errorMsg.set(this.i18n.translate('common.saveError')); this.saving.set(false); },
    });
  }
}
