import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-logo',
  templateUrl: './logo.component.html',
  styleUrl: './logo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LogoComponent {
  /** full = mark + wordmark, mark = mark only (short), wordmark = text only */
  variant = input<'full' | 'mark' | 'wordmark'>('full');
  size = input<'sm' | 'md' | 'lg'>('md');
}
