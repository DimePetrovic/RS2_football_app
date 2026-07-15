import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * Vizuelni indikator koraka za multistep forme (Scoreline stil).
 * Numbered circles + lines; a completed step gets a Volt fill and a checkmark.
 * Clicking an already-completed step emits `stepSelected` (jump backward).
 */
@Component({
  selector: 'app-stepper',
  templateUrl: './stepper.component.html',
  styleUrl: './stepper.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepperComponent {
  /** Labele koraka, redom */
  steps = input.required<string[]>();
  /** Active step (0-based) */
  current = input.required<number>();

  stepSelected = output<number>();

  onSelect(i: number): void {
    if (i < this.current()) this.stepSelected.emit(i);
  }
}
