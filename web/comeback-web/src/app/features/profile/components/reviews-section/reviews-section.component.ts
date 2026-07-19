import { ChangeDetectionStrategy, Component, Input, computed, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { PlayerBadgeComponent } from '../../../../shared/components/player-badge/player-badge.component';
import { PlayerReceivedReviewItem } from '../../../matches/models/match.models';

const COLLAPSED_COUNT = 3;

/** The "Reviews" profile section: received ratings with reviewer badge and show-all toggle. */
@Component({
  selector: 'app-reviews-section',
  imports: [DatePipe, MatButtonModule, MatIconModule, TranslatePipe, PlayerBadgeComponent],
  templateUrl: './reviews-section.component.html',
  styleUrl: './reviews-section.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReviewsSectionComponent {
  private readonly reviewsSignal = signal<PlayerReceivedReviewItem[]>([]);

  @Input({ required: true }) set reviews(value: PlayerReceivedReviewItem[]) {
    this.reviewsSignal.set(value ?? []);
  }
  get reviews(): PlayerReceivedReviewItem[] { return this.reviewsSignal(); }

  readonly showAll = signal(false);
  readonly all = computed(() => this.reviewsSignal());
  readonly visible = computed(() =>
    this.showAll() ? this.all() : this.all().slice(0, COLLAPSED_COUNT));
  readonly collapsible = computed(() => this.all().length > COLLAPSED_COUNT);
}
