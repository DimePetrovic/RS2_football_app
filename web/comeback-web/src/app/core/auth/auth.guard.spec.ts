import { TestBed } from '@angular/core/testing';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { provideRouter } from '@angular/router';
import { adminGuard, authGuard, guestGuard, playerAreaGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('auth guards', () => {
  let router: Router;

  /** Guards use inject(), so they have to run inside an injection context. */
  function run(guard: CanActivateFn): boolean | UrlTree {
    return TestBed.runInInjectionContext(
      () => guard(null!, null!),
    ) as boolean | UrlTree;
  }

  function configure(user: { role: string } | null) {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: () => user !== null,
            currentUser: () => user,
          },
        },
      ],
    });
    router = TestBed.inject(Router);
  }

  function path(result: boolean | UrlTree): string {
    return router.serializeUrl(result as UrlTree);
  }

  describe('authGuard', () => {
    it('lets a signed-in user through', () => {
      configure({ role: 'Player' });

      expect(run(authGuard)).toBeTrue();
    });

    it('sends an anonymous visitor to the login page', () => {
      configure(null);

      expect(path(run(authGuard))).toBe('/auth/login');
    });
  });

  describe('guestGuard', () => {
    it('lets an anonymous visitor through', () => {
      configure(null);

      expect(run(guestGuard)).toBeTrue();
    });

    it('sends a signed-in user to the feed instead of the login page', () => {
      configure({ role: 'Player' });

      expect(path(run(guestGuard))).toBe('/feed');
    });
  });

  describe('adminGuard', () => {
    it('lets an admin through', () => {
      configure({ role: 'Admin' });

      expect(run(adminGuard)).toBeTrue();
    });

    it('turns a regular player away from the admin area', () => {
      configure({ role: 'Player' });

      expect(path(run(adminGuard))).toBe('/feed');
    });

    it('turns an anonymous visitor away as well', () => {
      configure(null);

      expect(path(run(adminGuard))).toBe('/feed');
    });
  });

  describe('playerAreaGuard', () => {
    it('lets a player through', () => {
      configure({ role: 'Player' });

      expect(run(playerAreaGuard)).toBeTrue();
    });

    it('redirects an admin to the user administration', () => {
      configure({ role: 'Admin' });

      expect(path(run(playerAreaGuard))).toBe('/admin/users');
    });
  });
});
