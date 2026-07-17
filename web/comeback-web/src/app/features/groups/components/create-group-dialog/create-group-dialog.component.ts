import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { GroupService } from '../../services/group.service';
import { GroupSummary } from '../../models/group.models';
import { ProfileService } from '../../../profile/services/profile.service';
import { ProfileSearchResult } from '../../../profile/models/profile.models';
import { AvatarUploadComponent } from '../../../../shared/components/avatar-upload/avatar-upload.component';

@Component({
  selector: 'app-create-group-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatAutocompleteModule,
    TranslatePipe,
    AvatarUploadComponent,
  ],
  templateUrl: './create-group-dialog.component.html',
  styleUrl: './create-group-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateGroupDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly i18n = inject(TranslationService);
  private readonly groupService = inject(GroupService);
  private readonly profileService = inject(ProfileService);
  private readonly dialogRef = inject(MatDialogRef<CreateGroupDialogComponent>);

  readonly saving = signal(false);
  readonly errorMsg = signal<string | null>(null);
  readonly selectedMembers = signal<ProfileSearchResult[]>([]);

  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    avatarUrl: [''],
    memberSearch: [''],
  });

  onAvatarChange(url: string | null) {
    this.form.controls.avatarUrl.setValue(url ?? '');
  }

  readonly searchResults = toSignal(
    this.form.get('memberSearch')!.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(q => (q && q.length >= 2)
        ? this.profileService.searchProfiles(q)
        : of([])),
    ),
    { initialValue: [] as ProfileSearchResult[] }
  );

  readonly filteredResults = computed(() =>
    this.searchResults().filter(
      p => !this.selectedMembers().find(m => m.userId === p.userId)
    )
  );

  displayFn = (_: ProfileSearchResult) => '';

  selectMember(event: MatAutocompleteSelectedEvent) {
    const player: ProfileSearchResult = event.option.value;
    this.selectedMembers.update(list => [...list, player]);
    this.form.get('memberSearch')!.setValue('', { emitEvent: false });
  }

  removeMember(userId: string) {
    this.selectedMembers.update(list => list.filter(m => m.userId !== userId));
  }

  submit() {
    if (this.form.get('name')?.invalid) return;
    if (this.selectedMembers().length === 0) {
      this.errorMsg.set(this.i18n.translate('groups.createDialog.addAtLeastOne'));
      return;
    }
    this.saving.set(true);
    this.errorMsg.set(null);
    const val = this.form.value;
    this.groupService.createGroup({
      name: val.name!,
      avatarUrl: val.avatarUrl || null,
      memberUserIds: this.selectedMembers().map(m => m.userId),
    }).subscribe({
      next: (group: GroupSummary) => this.dialogRef.close(group),
      error: () => {
        this.errorMsg.set(this.i18n.translate('groups.createDialog.error'));
        this.saving.set(false);
      },
    });
  }
}
