import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CloudinaryUploadResult,
  CloudinaryUploadSignature,
  IMAGE_MAX_BYTES,
  IMAGE_TYPES,
  VIDEO_MAX_BYTES,
  VIDEO_TYPES,
} from './media.models';

/**
 * Direct browser upload to Cloudinary, signed by our backend.
 * The file does not pass through the gateway — the backend stores only metadata and the URL.
 */
@Injectable({ providedIn: 'root' })
export class MediaUploadService {
  private readonly http = inject(HttpClient);

  validate(file: File): string | null {
    if (IMAGE_TYPES.includes(file.type)) {
      return file.size <= IMAGE_MAX_BYTES ? null : 'media.errors.imageTooLarge';
    }
    if (VIDEO_TYPES.includes(file.type)) {
      return file.size <= VIDEO_MAX_BYTES ? null : 'media.errors.videoTooLarge';
    }
    return 'media.errors.unsupportedType';
  }

  isVideo(file: File): boolean {
    return VIDEO_TYPES.includes(file.type);
  }

  upload(sig: CloudinaryUploadSignature, file: File): Observable<CloudinaryUploadResult> {
    const form = new FormData();
    form.append('file', file);
    form.append('api_key', sig.apiKey);
    form.append('timestamp', String(sig.timestamp));
    form.append('folder', sig.folder);
    form.append('signature', sig.signature);

    return this.http.post<CloudinaryUploadResult>(
      `https://api.cloudinary.com/v1_1/${sig.cloudName}/auto/upload`, form);
  }

  /** Inserts a transformation into a Cloudinary delivery URL (…/upload/<transform>/…). */
  transformUrl(url: string, transform: string): string {
    return url.replace('/upload/', `/upload/${transform}/`);
  }

  /** Poster frame for a video — Cloudinary returns a thumbnail when the extension is replaced with .jpg. */
  videoThumbnail(videoUrl: string): string {
    return this.transformUrl(videoUrl.replace(/\.[^/.]+$/, '.jpg'), 'w_600,c_limit,q_auto');
  }
}
