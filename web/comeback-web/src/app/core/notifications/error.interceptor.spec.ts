import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { errorInterceptor, skipErrorToast } from './error.interceptor';
import { ToastService } from './toast.service';
import { TranslationService } from '../i18n/translation.service';
import { environment } from '../../../environments/environment';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let toast: jasmine.SpyObj<ToastService>;

  const url = `${environment.apiUrl}/api/matches`;

  /** Stands in for the loaded dictionary — returns the key so assertions stay readable. */
  const i18n = {
    translate: (key: string) => key,
    translateFlat: (prefix: string, code: string) =>
      code === 'match.player_already_in' ? 'Igrac je vec na mecu.' : null,
  };

  beforeEach(() => {
    toast = jasmine.createSpyObj('ToastService', ['error', 'success']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: ToastService, useValue: toast },
        { provide: TranslationService, useValue: i18n },
      ],
    });

    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  function fail(
    status: number,
    body: Record<string, unknown> | null = null,
    options: { skip?: boolean } = {},
  ) {
    http.get(url, options.skip ? { context: skipErrorToast() } : {}).subscribe({ error: () => {} });
    backend.expectOne(url).flush(body, { status, statusText: 'Error' });
  }

  it('prefers the backend error code over the generic HTTP message', () => {
    fail(400, { code: 'match.player_already_in' });

    expect(toast.error).toHaveBeenCalledWith('Igrac je vec na mecu.');
  });

  it('falls back to a status-specific message for an unknown code', () => {
    fail(404, { code: 'nepoznat.kod' });

    expect(toast.error).toHaveBeenCalledWith('errors.http.notFound');
  });

  it('reports a connection failure as offline', () => {
    fail(0);

    expect(toast.error).toHaveBeenCalledWith('errors.http.offline');
  });

  it('reports any 5xx as a server error', () => {
    fail(503);

    expect(toast.error).toHaveBeenCalledWith('errors.http.server');
  });

  it('stays silent on 401 — the auth interceptor owns that case', () => {
    fail(401);

    expect(toast.error).not.toHaveBeenCalled();
  });

  it('stays silent for auth endpoints, whose forms show errors inline', () => {
    const loginUrl = `${environment.apiUrl}/api/auth/login`;
    http.post(loginUrl, {}).subscribe({ error: () => {} });
    backend.expectOne(loginUrl).flush({ code: 'auth.invalid' }, { status: 400, statusText: 'Error' });

    expect(toast.error).not.toHaveBeenCalled();
  });

  it('stays silent when the caller opted out', () => {
    fail(400, { code: 'match.player_already_in' }, { skip: true });

    expect(toast.error).not.toHaveBeenCalled();
  });
});
