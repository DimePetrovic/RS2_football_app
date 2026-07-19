import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslationService } from '../../core/i18n/translation.service';

/** Localized label for a match status (falls back to the raw status for unknown values). */
@Pipe({ name: 'matchStatus', standalone: true, pure: false })
export class MatchStatusPipe implements PipeTransform {
  private readonly i18n = inject(TranslationService);

  transform(status: string | null | undefined): string {
    if (!status) return '';
    const key = `match.status.${status}`;
    const translated = this.i18n.translate(key);
    return translated === key ? status : translated;
  }
}
