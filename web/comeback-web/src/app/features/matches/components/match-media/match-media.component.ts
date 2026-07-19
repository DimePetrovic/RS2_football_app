import {
  ChangeDetectionStrategy, Component, Input, OnInit, computed, inject, signal,
} from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog } from '@angular/material/dialog';
import { forkJoin, switchMap } from 'rxjs';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { MediaUploadService } from '../../../../core/media/media-upload.service';
import { MatchService } from '../../services/match.service';
import { MatchMediaResponse } from '../../models/match.models';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

/**
 * Match media section (images and clips) — loads and manages its own
 * content; the parent passes only the match context and permissions.
 */
@Component({
  selector: 'app-match-media',
  imports: [
    MatTooltipModule,MatButtonModule, MatIconModule, MatDividerModule, TranslatePipe],
  templateUrl: './match-media.component.html',
  styleUrl: './match-media.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MatchMediaComponent implements OnInit {
  private readonly matchService = inject(MatchService);
  private readonly mediaUpload = inject(MediaUploadService);
  private readonly dialog = inject(MatDialog);

  @Input({ required: true }) matchId!: string;
  @Input() canAdd = false;
  @Input() currentUserId = '';
  @Input() isOrganizer = false;

  readonly media = signal<MatchMediaResponse[]>([]);
  readonly uploading = signal(false);
  readonly uploadProgress = signal<{ done: number; total: number } | null>(null);
  readonly error = signal<string | null>(null);

  readonly visible = computed(() => this.media().length > 0 || this.canAdd);

  ngOnInit() { this.load(); }

  canDelete(item: MatchMediaResponse): boolean {
    return item.uploadedByUserId === this.currentUserId || this.isOrganizer;
  }

  thumbnail(item: MatchMediaResponse): string {
    if (item.thumbnailUrl) return item.thumbnailUrl;
    return item.mediaType === 'Video'
      ? this.mediaUpload.videoThumbnail(item.url)
      : this.mediaUpload.transformUrl(item.url, 'w_600,c_limit,q_auto');
  }

  load() {
    this.matchService.getMedia(this.matchId).subscribe({
      next: (items) => this.media.set(items),
    });
  }

  onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';
    if (files.length === 0) return;

    const invalid = files.map(f => this.mediaUpload.validate(f)).find(e => e !== null);
    if (invalid) {
      this.error.set(invalid);
      return;
    }

    this.error.set(null);
    this.uploading.set(true);
    this.uploadProgress.set({ done: 0, total: files.length });

    // One signature covers the whole batch — all files go to the same match folder.
    this.matchService.getMediaUploadSignature(this.matchId).pipe(
      switchMap((sig) => forkJoin(files.map(file =>
        this.mediaUpload.upload(sig, file).pipe(
          switchMap((result) => {
            this.uploadProgress.update(p => p && { ...p, done: p.done + 1 });
            return this.matchService.addMedia(this.matchId, {
              mediaType: result.resource_type === 'video' ? 'Video' : 'Image',
              storagePublicId: result.public_id,
              url: result.secure_url,
              thumbnailUrl: result.resource_type === 'video'
                ? this.mediaUpload.videoThumbnail(result.secure_url)
                : null,
              format: result.format ?? null,
              sizeInBytes: result.bytes,
              durationInSeconds: result.duration ?? null,
              width: result.width ?? null,
              height: result.height ?? null,
            });
          }),
        )))),
    ).subscribe({
      next: () => {
        this.uploading.set(false);
        this.uploadProgress.set(null);
        this.load();
      },
      error: () => {
        this.uploading.set(false);
        this.uploadProgress.set(null);
        this.error.set('media.errors.uploadFailed');
        this.load();
      },
    });
  }

  delete(item: MatchMediaResponse) {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      width: '360px',
      data: {
        titleKey: 'match.media.deleteDialog.title',
        messageKey: 'match.media.deleteDialog.message',
        confirmLabelKey: 'match.media.deleteDialog.confirm',
        confirmColor: 'warn',
      },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.matchService.deleteMedia(this.matchId, item.id).subscribe({
        next: () => this.load(),
      });
    });
  }
}
