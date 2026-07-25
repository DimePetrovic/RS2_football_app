import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TranslationService } from './translation.service';

describe('TranslationService', () => {
  let service: TranslationService;
  let http: HttpTestingController;

  const dictionary = {
    auth: { login: { title: 'Prijava' } },
    greeting: 'Zdravo {{name}}, imas {{count}} poruka',
    errors: {
      codes: {
        // Error codes are flat keys that themselves contain dots.
        'match.player_already_in': 'Igrac je vec na mecu.',
      },
    },
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TranslationService);
    http = TestBed.inject(HttpTestingController);

    service.load('sr-latn').subscribe();
    http.expectOne('/i18n/sr-latn.json').flush(dictionary);
  });

  afterEach(() => http.verify());

  describe('translate', () => {
    it('resolves a nested key', () => {
      expect(service.translate('auth.login.title')).toBe('Prijava');
    });

    it('substitutes parameters', () => {
      expect(service.translate('greeting', { name: 'Petar', count: 3 }))
        .toBe('Zdravo Petar, imas 3 poruka');
    });

    it('leaves a placeholder untouched when the parameter is missing', () => {
      expect(service.translate('greeting', { name: 'Petar' }))
        .toBe('Zdravo Petar, imas {{count}} poruka');
    });

    it('returns the key itself when there is no translation', () => {
      expect(service.translate('auth.login.nepostojece')).toBe('auth.login.nepostojece');
    });

    it('returns the key when it resolves to an object instead of a string', () => {
      expect(service.translate('auth.login')).toBe('auth.login');
    });
  });

  describe('translateFlat', () => {
    it('finds a key that contains dots, which translate() would split', () => {
      expect(service.translateFlat('errors.codes', 'match.player_already_in'))
        .toBe('Igrac je vec na mecu.');

      // This is the whole reason translateFlat exists.
      expect(service.translate('errors.codes.match.player_already_in'))
        .toBe('errors.codes.match.player_already_in');
    });

    it('returns null for an unknown code so the caller can fall back', () => {
      expect(service.translateFlat('errors.codes', 'nepoznat.kod')).toBeNull();
    });

    it('returns null when the prefix itself does not exist', () => {
      expect(service.translateFlat('errors.nepostojece', 'bilo.sta')).toBeNull();
    });
  });
});
