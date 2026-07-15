import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/**
 * Single place to show short messages to the user (error/success).
 * Uses the global cb-snack-error / cb-snack-success styles from styles.scss.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly snackBar = inject(MatSnackBar);

  error(message: string) {
    this.snackBar.open(message, 'OK', {
      duration: 5000,
      panelClass: 'cb-snack-error',
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
    });
  }

  success(message: string) {
    this.snackBar.open(message, undefined, {
      duration: 3000,
      panelClass: 'cb-snack-success',
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
    });
  }
}
