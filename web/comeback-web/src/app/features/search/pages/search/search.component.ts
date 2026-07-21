import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { catchError, debounceTime, distinctUntilChanged, map, of, startWith, switchMap } from 'rxjs';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { ProfileService } from '../../../profile/services/profile.service';
import { ProfileSearchResult } from '../../../profile/models/profile.models';
import { PlayerBadgeComponent } from '../../../../shared/components/player-badge/player-badge.component';

const MIN_QUERY = 2;

interface SearchView {
  status: 'idle' | 'loading' | 'results' | 'empty';
  results: ProfileSearchResult[];
}

@Component({
  selector: 'app-search',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TranslatePipe,
    PlayerBadgeComponent,
  ],
  templateUrl: './search.component.html',
  styleUrl: './search.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchComponent {
  private readonly profileService = inject(ProfileService);

  readonly query = new FormControl('', { nonNullable: true });

  // Single source of truth for the view: idle (too short) -> loading -> results/empty.
  readonly view = toSignal(
    this.query.valueChanges.pipe(
      map((q) => q.trim()),
      debounceTime(300),
      distinctUntilChanged(),
      switchMap((q) =>
        q.length < MIN_QUERY ? of<SearchView>({ status: 'idle', results: [] }) : this.searchFor(q)),
      startWith<SearchView>({ status: 'idle', results: [] }),
    ),
    { initialValue: { status: 'idle', results: [] } as SearchView },
  );

  private searchFor(q: string) {
    return this.profileService.searchProfiles(q).pipe(
      map((results): SearchView => ({ status: results.length ? 'results' : 'empty', results })),
      startWith<SearchView>({ status: 'loading', results: [] }),
      catchError(() => of<SearchView>({ status: 'empty', results: [] })),
    );
  }
}
