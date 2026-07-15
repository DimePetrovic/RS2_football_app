import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TranslationService {
  private readonly http = inject(HttpClient);
  private translations = signal<Record<string, unknown>>({});

  load(locale: string) {
    return this.http
      .get<Record<string, unknown>>(`/i18n/${locale}.json`)
      .pipe(tap((t) => this.translations.set(t)));
  }

  /**
   * Looks up a flat key (which may itself contain dots, e.g. an error code like
   * "match.player_already_in") inside the object at `prefix`. The regular translate()
   * would split the code on dots and miss it.
   */
  translateFlat(prefix: string, flatKey: string): string | null {
    let node: unknown = this.translations();
    for (const k of prefix.split('.')) {
      if (node == null || typeof node !== 'object') return null;
      node = (node as Record<string, unknown>)[k];
    }
    if (node == null || typeof node !== 'object') return null;
    const value = (node as Record<string, unknown>)[flatKey];
    return typeof value === 'string' ? value : null;
  }

  translate(key: string, params?: Record<string, string | number>): string {
    const keys = key.split('.');
    let value: unknown = this.translations();

    for (const k of keys) {
      if (value == null || typeof value !== 'object') return key;
      value = (value as Record<string, unknown>)[k];
    }

    if (typeof value !== 'string') return key;

    if (!params) return value;

    return value.replace(/\{\{(\w+)\}\}/g, (_, name) =>
      String(params[name] ?? `{{${name}}}`)
    );
  }
}
