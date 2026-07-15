import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslationService } from './translation.service';

@Pipe({ name: 'translate', standalone: true, pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly t = inject(TranslationService);

  transform(key: string | null | undefined, params?: Record<string, string | number>): string {
    if (!key) return '';

    return this.t.translate(key, params);
  }
}
