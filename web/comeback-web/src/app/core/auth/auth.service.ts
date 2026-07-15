import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { map, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  CompleteRegistrationRequest,
  CurrentUser,
  LoginRequest,
  RefreshResponse,
  RegisterRequest,
  RegisterResponse,
  ResendConfirmationRequest,
  ValidateEmailTokenResponse,
} from './models/auth.models';

const USER_KEY = 'cb_user';
const TOKEN_KEY = 'cb_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly base = environment.apiUrl;

  private readonly _accessToken = signal<string | null>(
    localStorage.getItem(TOKEN_KEY)
  );
  private readonly _currentUser = signal<CurrentUser | null>(
    this.loadUser()
  );

  readonly accessToken = this._accessToken.asReadonly();
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => !!this._accessToken());

  private refreshTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    // Refresh proactively so parallel requests never fire with an expired token
    // (avoids the 401 burst on app load and keeps SignalR negotiates valid).
    this.scheduleProactiveRefresh();
  }

  register(req: RegisterRequest) {
    return this.http.post<RegisterResponse>(
      `${this.base}/api/auth/register`,
      req
    );
  }

  login(req: LoginRequest) {
    return this.http
      .post<AuthResponse>(`${this.base}/api/auth/login`, req, {
        withCredentials: true,
      })
      .pipe(tap((res) => this.storeSession(res)));
  }

  validateEmailToken(userId: string, token: string) {
    return this.http.get<ValidateEmailTokenResponse>(
      `${this.base}/api/auth/validate-email-token?userId=${encodeURIComponent(userId)}&token=${encodeURIComponent(token)}`
    );
  }

  completeRegistration(req: CompleteRegistrationRequest) {
    return this.http
      .post<AuthResponse>(`${this.base}/api/auth/complete-registration`, req, { withCredentials: true })
      .pipe(tap((res) => this.storeSession(res)));
  }

  resendConfirmation(req: ResendConfirmationRequest) {
    return this.http.post<void>(`${this.base}/api/auth/resend-confirmation`, req);
  }

  refresh() {
    return this.http
      .post<RefreshResponse>(`${this.base}/api/auth/refresh`, {}, { withCredentials: true })
      .pipe(
        tap((res) => this.storeSession({ ...res, refreshToken: '', refreshTokenExpiresAt: '' })),
        map((res) => res.accessToken)
      );
  }

  logout() {
    this.clearSession();
    this.router.navigate(['/auth/login']);
  }

  private storeSession(res: AuthResponse) {
    const user: CurrentUser = {
      userId: res.userId,
      username: res.username,
      email: res.email,
      role: res.role,
    };
    localStorage.setItem(TOKEN_KEY, res.accessToken);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this._accessToken.set(res.accessToken);
    this._currentUser.set(user);
    this.scheduleProactiveRefresh();
  }

  private clearSession() {
    clearTimeout(this.refreshTimer);
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._accessToken.set(null);
    this._currentUser.set(null);
  }

  private scheduleProactiveRefresh() {
    clearTimeout(this.refreshTimer);
    const token = this._accessToken();
    if (!token) return;

    const expiresAt = this.tokenExpiresAt(token);
    if (!expiresAt) return;

    const delay = expiresAt - Date.now() - 60_000;
    if (delay <= 0) {
      // Already (nearly) expired — refresh now; failures fall through to the 401/logout path.
      this.refresh().subscribe({ error: () => {} });
      return;
    }
    this.refreshTimer = setTimeout(
      () => this.refresh().subscribe({ error: () => {} }), delay);
  }

  private tokenExpiresAt(token: string): number | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1])) as { exp?: number };
      return typeof payload.exp === 'number' ? payload.exp * 1000 : null;
    } catch {
      return null;
    }
  }

  private loadUser(): CurrentUser | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? (JSON.parse(raw) as CurrentUser) : null;
    } catch {
      return null;
    }
  }
}
