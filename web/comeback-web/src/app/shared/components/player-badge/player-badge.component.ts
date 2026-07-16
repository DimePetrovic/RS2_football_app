import { ChangeDetectionStrategy, Component, Input, computed, inject, signal } from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslationService } from '../../../core/i18n/translation.service';
import { flagClass } from '../../../core/countries/countries';
import { RouterLink } from '@angular/router';

/**
 * Compact player display: avatar (or initials), first and last name, @username.
 * Used everywhere a player is shown in a list, search, or chip.
 * When a userId is provided, clicking navigates to the player's profile.
 */
@Component({
  selector: 'app-player-badge',
  imports: [RouterLink, MatTooltipModule],
  templateUrl: './player-badge.component.html',
  styleUrl: './player-badge.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlayerBadgeComponent {
  private readonly i18n = inject(TranslationService);
  private readonly usernameSignal = signal<string | null>(null);
  private readonly nameSignal = signal<string | null>(null);

  @Input() avatarUrl: string | null = null;
  @Input() userId: string | null = null;
  @Input() size: 'sm' | 'md' = 'md';

  private readonly countrySignal = signal<string | null>(null);
  /** ISO 3166-1 alpha-2 code; renders the flag next to the name when present. */
  @Input() set countryCode(value: string | null | undefined) {
    this.countrySignal.set(value ?? null);
  }
  readonly flag = computed(() => flagClass(this.countrySignal()));
  readonly countryName = computed(() => {
    const code = this.countrySignal();
    if (!code) return '';
    const key = `countries.${code.toUpperCase()}`;
    const translated = this.i18n.translate(key);
    return translated === key ? code : translated;
  });

  /** Username; guests without an account have none — then only the name is shown. */
  @Input() set username(value: string | null | undefined) {
    this.usernameSignal.set(value?.trim() || null);
  }
  get username(): string | null { return this.usernameSignal(); }

  /** First and last name (or display name); when missing, only @username is shown. */
  @Input() set name(value: string | null | undefined) {
    this.nameSignal.set(value?.trim() || null);
  }
  get name(): string | null { return this.nameSignal(); }

  readonly initials = computed(() => {
    const source = this.nameSignal() ?? this.usernameSignal() ?? '?';
    const parts = source.trim().split(/\s+/);
    return parts.length >= 2
      ? (parts[0][0] + parts[1][0]).toUpperCase()
      : source.substring(0, 2).toUpperCase();
  });
}
