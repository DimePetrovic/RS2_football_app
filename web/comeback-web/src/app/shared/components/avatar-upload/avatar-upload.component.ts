import {
  ChangeDetectionStrategy, Component, ElementRef, EventEmitter,
  Input, Output, ViewChild, inject, signal,
} from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { switchMap } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { MediaUploadService } from '../../../core/media/media-upload.service';
import { IMAGE_TYPES } from '../../../core/media/media.models';
import { ProfileService } from '../../../features/profile/services/profile.service';

@Component({
  selector: 'app-avatar-upload',
  imports: [
    MatTooltipModule,MatButtonModule, MatIconModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './avatar-upload.component.html',
  styleUrl: './avatar-upload.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AvatarUploadComponent {
  private readonly profileService = inject(ProfileService);
  private readonly mediaUpload = inject(MediaUploadService);

  @Input() url: string | null = null;
  @Input() icon = 'person';
  @Output() urlChange = new EventEmitter<string | null>();

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  readonly uploading = signal(false);
  readonly errorKey = signal<string | null>(null);

  readonly acceptTypes = IMAGE_TYPES.join(',');

  pickFile() {
    this.fileInput.nativeElement.click();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    const error = this.mediaUpload.validate(file);
    if (error || this.mediaUpload.isVideo(file)) {
      this.errorKey.set(error ?? 'media.errors.unsupportedType');
      return;
    }

    this.errorKey.set(null);
    this.uploading.set(true);
    this.profileService.getAvatarUploadSignature().pipe(
      switchMap((sig) => this.mediaUpload.upload(sig, file)),
    ).subscribe({
      next: (result) => {
        this.uploading.set(false);
        this.urlChange.emit(result.secure_url);
      },
      error: () => {
        this.uploading.set(false);
        this.errorKey.set('media.errors.uploadFailed');
      },
    });
  }

  removeAvatar() {
    this.errorKey.set(null);
    this.urlChange.emit(null);
  }
}
