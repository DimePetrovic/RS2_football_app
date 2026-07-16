import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

/**
 * The single confirmation dialog for the whole app. Callers pass i18n keys
 * (never raw text) plus optional interpolation params for the message.
 */
export interface ConfirmDialogData {
  titleKey: string;
  messageKey: string;
  messageParams?: Record<string, string | number>;
  confirmLabelKey?: string;
  cancelLabelKey?: string;
  confirmColor?: 'primary' | 'warn';
}

@Component({
  selector: 'app-confirm-dialog',
  imports: [MatDialogModule, MatButtonModule, TranslatePipe],
  template: `
    <h2 mat-dialog-title>{{ data.titleKey | translate }}</h2>
    <mat-dialog-content>
      <p>{{ data.messageKey | translate: data.messageParams }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">
        {{ (data.cancelLabelKey ?? 'common.cancel') | translate }}
      </button>
      <button mat-flat-button [color]="data.confirmColor ?? 'warn'" [mat-dialog-close]="true">
        {{ (data.confirmLabelKey ?? 'common.confirm') | translate }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content p { margin: 0; color: var(--cb-text-muted); }
    mat-dialog-actions { gap: 8px; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialogComponent {
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
}
