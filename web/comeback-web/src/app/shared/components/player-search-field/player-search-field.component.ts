import {
  ChangeDetectionStrategy, Component, EventEmitter, Input, Output,
  computed, inject, signal,
} from '@angular/core';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Subject, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { ProfileService } from '../../../features/profile/services/profile.service';
import { ProfileSearchResult } from '../../../features/profile/models/profile.models';
import { PlayerBadgeComponent } from '../player-badge/player-badge.component';

/**
 * The app-wide player search: debounced profile lookup rendered as badge options.
 * Parents pass a label key and (optionally) user ids to exclude; the field clears
 * itself after each selection so it can be used for building lists.
 */
@Component({
  selector: 'app-player-search-field',
  imports: [
    MatFormFieldModule, MatInputModule, MatIconModule, MatAutocompleteModule,
    TranslatePipe, PlayerBadgeComponent,
  ],
  template: `
    <mat-form-field appearance="outline" class="player-search-field">
      <mat-label>{{ labelKey | translate }}</mat-label>
      <input matInput
        [value]="query()"
        (input)="onSearch($any($event.target).value)"
        [matAutocomplete]="auto"
        [placeholder]="placeholderKey | translate"
        autocomplete="off" />
      <mat-icon matSuffix>search</mat-icon>
      <mat-autocomplete #auto [displayWith]="displayEmpty" (optionSelected)="select($event)">
        @for (p of results(); track p.userId) {
          <mat-option [value]="p">
            <app-player-badge size="sm"
              [avatarUrl]="p.avatarUrl" [username]="p.username"
              [name]="p.firstName + ' ' + p.lastName" [countryCode]="p.nationality" />
          </mat-option>
        }
        @if (results().length === 0 && query().length >= 2) {
          <mat-option disabled>{{ 'common.noResults' | translate }}</mat-option>
        }
      </mat-autocomplete>
    </mat-form-field>
  `,
  styles: [`.player-search-field { width: 100%; }`],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlayerSearchFieldComponent {
  private readonly profileService = inject(ProfileService);

  @Input({ required: true }) labelKey!: string;
  @Input() placeholderKey = 'common.searchByName';

  private readonly excludedSignal = signal<ReadonlySet<string>>(new Set());
  /** User ids hidden from the results (already selected, already members, self…). */
  @Input() set excludedIds(value: readonly string[] | null | undefined) {
    this.excludedSignal.set(new Set(value ?? []));
  }

  @Output() playerSelected = new EventEmitter<ProfileSearchResult>();

  readonly query = signal('');
  private readonly rawResults = signal<ProfileSearchResult[]>([]);
  readonly results = computed(() => {
    const excluded = this.excludedSignal();
    return this.rawResults().filter(p => !excluded.has(p.userId));
  });

  private readonly search$ = new Subject<string>();

  constructor() {
    this.search$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(q => this.profileService.searchProfiles(q)),
      takeUntilDestroyed(),
    ).subscribe({ next: results => this.rawResults.set(results) });
  }

  onSearch(q: string) {
    this.query.set(q);
    if (q.length >= 2) this.search$.next(q);
    else this.rawResults.set([]);
  }

  select(event: MatAutocompleteSelectedEvent) {
    const player = event.option.value as ProfileSearchResult;
    this.query.set('');
    this.rawResults.set([]);
    this.playerSelected.emit(player);
  }

  displayEmpty = () => '';
}
