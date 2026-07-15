import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

let isRefreshing = false;
const refreshToken$ = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  // The token goes only to our API — external services (e.g. Cloudinary upload) must not receive it.
  if (!req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }

  return next(addToken(req, auth.accessToken())).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && !req.url.includes('/api/auth/')) {
        return handle401(req, next, auth);
      }
      return throwError(() => err);
    })
  );
};

function addToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;
}

function handle401(req: HttpRequest<unknown>, next: HttpHandlerFn, auth: AuthService) {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshToken$.next(null);

    return auth.refresh().pipe(
      switchMap((newToken) => {
        isRefreshing = false;
        refreshToken$.next(newToken);
        return next(addToken(req, newToken));
      }),
      catchError((err) => {
        isRefreshing = false;
        refreshToken$.next(null);
        auth.logout();
        return throwError(() => err);
      })
    );
  }

  // Wait for the current refresh to finish, then retry the request
  return refreshToken$.pipe(
    filter((token): token is string => token !== null),
    take(1),
    switchMap((token) => next(addToken(req, token)))
  );
}
