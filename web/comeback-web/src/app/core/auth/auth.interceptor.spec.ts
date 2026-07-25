import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: jasmine.SpyObj<Pick<AuthService, 'accessToken' | 'refresh' | 'logout'>>;

  const apiUrl = `${environment.apiUrl}/api/profiles/me`;

  beforeEach(() => {
    auth = jasmine.createSpyObj('AuthService', ['refresh', 'logout'], {
      accessToken: () => 'token-123',
    });

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
      ],
    });

    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('attaches the bearer token to our own API', () => {
    http.get(apiUrl).subscribe();

    const request = backend.expectOne(apiUrl);
    expect(request.request.headers.get('Authorization')).toBe('Bearer token-123');
    request.flush({});
  });

  it('never leaks the token to a third-party host', () => {
    // Media goes straight to Cloudinary — sending our access token there would hand
    // a valid credential to an external service.
    http.post('https://api.cloudinary.com/v1_1/demo/image/upload', {}).subscribe();

    const request = backend.expectOne('https://api.cloudinary.com/v1_1/demo/image/upload');
    expect(request.request.headers.has('Authorization')).toBeFalse();
    request.flush({});
  });

  it('refreshes the token on 401 and replays the request with the new one', () => {
    auth.refresh.and.returnValue(of('token-456') as ReturnType<AuthService['refresh']>);

    http.get(apiUrl).subscribe();

    backend.expectOne(apiUrl).flush(null, { status: 401, statusText: 'Unauthorized' });

    const retried = backend.expectOne(apiUrl);
    expect(retried.request.headers.get('Authorization')).toBe('Bearer token-456');
    expect(auth.refresh).toHaveBeenCalledTimes(1);
    retried.flush({});
  });

  it('logs the user out when the refresh itself fails', (done) => {
    auth.refresh.and.returnValue(
      throwError(() => new Error('refresh failed')) as ReturnType<AuthService['refresh']>,
    );

    http.get(apiUrl).subscribe({
      error: () => {
        expect(auth.logout).toHaveBeenCalledTimes(1);
        done();
      },
    });

    backend.expectOne(apiUrl).flush(null, { status: 401, statusText: 'Unauthorized' });
  });

  it('does not try to refresh when the auth endpoints themselves return 401', (done) => {
    const loginUrl = `${environment.apiUrl}/api/auth/login`;

    http.post(loginUrl, {}).subscribe({
      error: () => {
        // A failed login must surface to the form, not trigger a refresh loop.
        expect(auth.refresh).not.toHaveBeenCalled();
        done();
      },
    });

    backend.expectOne(loginUrl).flush(null, { status: 401, statusText: 'Unauthorized' });
  });
});
