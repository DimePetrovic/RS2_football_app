import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app.component';
import { ThemeService } from './core/theme/theme.service';

describe('AppComponent', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      // AppComponent injects TranslationService (which needs HttpClient) and renders a
      // <router-outlet>, so the test module has to supply both.
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('loads the Serbian translations on init', () => {
    const fixture = TestBed.createComponent(AppComponent);

    fixture.detectChanges();

    const request = http.expectOne('/i18n/sr-latn.json');
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('applies the stored theme to the document on init', () => {
    TestBed.inject(ThemeService).set('light');
    const fixture = TestBed.createComponent(AppComponent);

    fixture.detectChanges();

    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    http.expectOne('/i18n/sr-latn.json').flush({});
  });
});
