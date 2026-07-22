import { Injectable, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslationService } from '../../../core/i18n/translation.service';
import { NotificationResponse } from '../models/notification.models';

/** Localized title/body rendered from a notification's type + payload (no user-facing text comes from the backend). */
export interface PresentedNotification {
  title: string;
  body: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationPresenterService {
  private readonly i18n = inject(TranslationService);
  private readonly datePipe = new DatePipe('en-US');

  present(n: NotificationResponse): PresentedNotification {
    // Rows from before the type+payload re-architecture carry ready-made text; render it as-is.
    if (n.legacyTitle) {
      return { title: n.legacyTitle, body: n.legacyBody ?? '' };
    }

    const payload = this.parse(n.payload);
    const base = `notifications.types.${n.type}`;

    const params: Record<string, string | number> = {
      matchTitle: this.str(payload['matchTitle']),
      organizerName: this.str(payload['organizerName']),
      organizerGroupName: this.str(payload['organizerGroupName']),
      opponentGroupName: this.str(payload['opponentGroupName']),
      responderName: this.str(payload['responderName']),
      playerName: this.str(payload['playerName']),
      score: this.score(payload),
      date: this.date(payload['startsAt']),
      location: this.str(payload['location']),
      subject: this.wantedSubject(payload['position']),
    };

    // Events with an optional location use a location-free body variant when it is missing.
    const hasLocation = typeof payload['location'] === 'string' && payload['location'] !== '';
    const bodyKey = hasLocation ? `${base}.body` : this.firstKey(`${base}.bodyNoLocation`, `${base}.body`);

    return {
      title: this.translateOr(`${base}.title`, params, n.type),
      body: this.translateOr(bodyKey, params, ''),
    };
  }

  private wantedSubject(position: unknown): string {
    if (typeof position !== 'string' || position === '') return this.i18n.translate('feed.wanted.anyPlayer');
    const key = `feed.wanted.positions.${position}`;
    const translated = this.i18n.translate(key);
    return translated === key ? this.i18n.translate('feed.wanted.anyPlayer') : translated;
  }

  private parse(raw: string | null): Record<string, unknown> {
    if (!raw) return {};
    try {
      const parsed: unknown = JSON.parse(raw);
      return typeof parsed === 'object' && parsed !== null ? (parsed as Record<string, unknown>) : {};
    } catch {
      return {};
    }
  }

  private firstKey(preferred: string, fallback: string): string {
    return this.i18n.translate(preferred) === preferred ? fallback : preferred;
  }

  private translateOr(key: string, params: Record<string, string | number>, fallback: string): string {
    const translated = this.i18n.translate(key, params);
    return translated === key ? fallback : translated;
  }

  private str(value: unknown): string {
    return typeof value === 'string' ? value : '';
  }

  private score(payload: Record<string, unknown>): string {
    const home = payload['homeScore'];
    const away = payload['awayScore'];
    if (typeof home === 'number' && typeof away === 'number') return `${home}:${away}`;
    return '';
  }

  private date(value: unknown): string {
    if (typeof value !== 'string') return '';
    return this.datePipe.transform(value, 'd. MMM, HH:mm') ?? '';
  }
}
