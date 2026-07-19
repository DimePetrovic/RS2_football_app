import {
  ChangeDetectionStrategy, Component, Input, computed, signal,
} from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

export interface StatsChartBucket {
  label: string;
  wins: number;
  draws: number;
  losses: number;
}

interface BarGeometry {
  bucket: StatsChartBucket;
  total: number;
  x: number;
  barWidth: number;
  winsPath: string;
  drawsPath: string;
  lossesPath: string;
  showLabel: boolean;
}

const PLOT_WIDTH = 640;
const PLOT_HEIGHT = 200;
const PAD_LEFT = 30;
const PAD_BOTTOM = 22;
const PAD_TOP = 6;
const RADIUS = 4;
const SEGMENT_GAP = 2;

/**
 * Stacked bar chart of outcomes: wins / draws / losses.
 * Boje su validirane dataviz procedurom za tamne podloge (#22252C / #181B20).
 */
@Component({
  selector: 'app-stats-chart',
  imports: [TranslatePipe],
  templateUrl: './stats-chart.component.html',
  styleUrl: './stats-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatsChartComponent {
  private readonly bucketsSignal = signal<StatsChartBucket[]>([]);

  @Input({ required: true }) set buckets(value: StatsChartBucket[]) {
    this.bucketsSignal.set(value);
    this.hovered.set(null);
  }

  readonly viewBox = `0 0 ${PLOT_WIDTH} ${PLOT_HEIGHT}`;
  readonly plotWidth = PLOT_WIDTH;
  readonly plotHeight = PLOT_HEIGHT;
  readonly baseline = PLOT_HEIGHT - PAD_BOTTOM;

  readonly hovered = signal<number | null>(null);
  readonly showTable = signal(false);

  private total(b: StatsChartBucket): number {
    return b.wins + b.draws + b.losses;
  }

  readonly yMax = computed(() =>
    Math.max(1, ...this.bucketsSignal().map(b => this.total(b))));

  readonly yTicks = computed(() => {
    const max = this.yMax();
    const step = Math.max(1, Math.ceil(max / 3));
    const ticks: { value: number; y: number }[] = [];
    for (let v = step; v <= max; v += step) ticks.push({ value: v, y: this.yFor(v) });
    return ticks;
  });

  readonly bars = computed<BarGeometry[]>(() => {
    const buckets = this.bucketsSignal();
    if (buckets.length === 0) return [];

    const innerWidth = PLOT_WIDTH - PAD_LEFT - 8;
    const slot = innerWidth / buckets.length;
    const barWidth = Math.max(4, Math.min(36, slot * 0.7));
    const labelEvery = buckets.length > 12 ? 5 : 1;

    return buckets.map((bucket, i) => {
      const x = PAD_LEFT + slot * i + (slot - barWidth) / 2;

      // Segments from the base upward: wins, draws, losses.
      // Only the top of the highest non-empty segment is rounded; segments are 2px apart.
      const segments = [bucket.wins, bucket.draws, bucket.losses];
      const topIndex = segments.reduce((top, v, idx) => (v > 0 ? idx : top), -1);

      const paths = ['', '', ''];
      let cumulative = 0;
      for (let s = 0; s < 3; s++) {
        const value = segments[s];
        if (value <= 0) continue;
        const yBottom = this.yFor(cumulative) - (cumulative > 0 ? SEGMENT_GAP : 0);
        const yTop = this.yFor(cumulative + value);
        const height = yBottom - yTop;
        if (height > 0) paths[s] = this.column(x, yTop, barWidth, height, s === topIndex);
        cumulative += value;
      }

      return {
        bucket,
        total: this.total(bucket),
        x, barWidth,
        winsPath: paths[0], drawsPath: paths[1], lossesPath: paths[2],
        showLabel: i % labelEvery === 0,
      };
    });
  });

  hoveredBar = computed(() => {
    const i = this.hovered();
    return i === null ? null : this.bars()[i] ?? null;
  });

  tooltipX(bar: BarGeometry): number {
    return Math.min(82, Math.max(0, ((bar.x + bar.barWidth / 2) / PLOT_WIDTH) * 100));
  }

  private yFor(value: number): number {
    const inner = this.baseline - PAD_TOP;
    return this.baseline - (value / this.yMax()) * inner;
  }

  private column(x: number, y: number, w: number, h: number, roundedTop: boolean): string {
    if (h <= 0) return '';
    const r = roundedTop ? Math.min(RADIUS, w / 2, h) : 0;
    return `M ${x} ${y + h}
            L ${x} ${y + r}
            Q ${x} ${y} ${x + r} ${y}
            L ${x + w - r} ${y}
            Q ${x + w} ${y} ${x + w} ${y + r}
            L ${x + w} ${y + h} Z`;
  }
}
