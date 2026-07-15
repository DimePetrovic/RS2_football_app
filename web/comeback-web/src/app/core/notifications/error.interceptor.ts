import { HttpContext, HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from './toast.service';
import { TranslationService } from '../i18n/translation.service';

/** Set on a request whose error the component shows inline, to avoid a duplicate toast. */
export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

export function skipErrorToast(): HttpContext {
  return new HttpContext().set(SKIP_ERROR_TOAST, true);
}

/**
 * Global safety net: no failed HTTP call passes silently.
 * Skips 401 (handled by the auth interceptor), /api/auth/ (forms show inline) and explicit opt-out.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const i18n = inject(TranslationService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const skip = req.context.get(SKIP_ERROR_TOAST)
        || err.status === 401
        || req.url.includes('/api/auth/');

      if (!skip) toast.error(errorMessage(err, i18n));

      return throwError(() => err);
    }),
  );
};

/** Maps the backend error `code` to a localized message; falls back to a generic message by HTTP status. */
export function errorMessage(err: HttpErrorResponse, i18n: TranslationService): string {
  const code: unknown = err.error?.code;
  if (typeof code === 'string' && code) {
    // Codes are flat keys containing dots — a nested translate() lookup would split them.
    const translated = i18n.translateFlat('errors.codes', code);
    if (translated) return translated;
  }

  if (err.status === 0) return i18n.translate('errors.http.offline');
  if (err.status === 403) return i18n.translate('errors.http.forbidden');
  if (err.status === 404) return i18n.translate('errors.http.notFound');
  if (err.status >= 500) return i18n.translate('errors.http.server');
  return i18n.translate('errors.http.generic');
}
